const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const path = require('node:path');
const cwd = path.resolve(__dirname, '..');
assert.match(process.env.COMPOSE_PROJECT_NAME || '', /^dbdverify-l10-/);
const prefix = `live-${Date.now()}`;
let checks = 0;
function command(program, args, expectedCode = 0) {
    console.log('COMMAND:', program, ...args);
    const result = spawnSync(program, args, { cwd, encoding: 'utf8', timeout: 180000 });
    process.stdout.write(result.stdout || '');
    process.stderr.write(result.stderr || '');
    assert.equal(result.status, expectedCode, result.error?.message || result.stderr);
    return result.stdout;
}
function run(args, code = 0) {
    return command('bash', ['run.sh', ...args], code).trim().split('\n').map(line => JSON.parse(line));
}
function check(name, condition) { assert.ok(condition, name); checks++; console.log('PASS', name); }
try {
    command('bash', ['run.sh', 'up']);
    const initial = run(['status'])[0];
    const primary = initial.members.find(member => member.state === 'PRIMARY').name.split(':')[0];
    const others = initial.members.filter(member => member.state === 'SECONDARY').map(member => member.name.split(':')[0]);
    check('majority write acknowledged', run(['write', `${prefix}-majority`, 'majority'])[0].outcome === 'Acknowledged');
    check('w1 write acknowledged', run(['write', `${prefix}-one`, '1'])[0].outcome === 'Acknowledged');
    for (const preference of ['primary', 'secondary']) {
        check(`${preference} reads written document`, run(['read', `${prefix}-majority`, preference])[0].outcome === 'Present');
    }
    check('successful verification matches', run(['verify', `${prefix}-majority`])[0].outcome === 'PresentMatches');
    check('successful missing read is absent', run(['verify', `${prefix}-missing`])[0].outcome === 'AbsentAtVerification');
    check('duplicate insert is rejected', run(['write', `${prefix}-majority`, 'majority'], 1)[0].outcome === 'RejectedWrite');
    command('docker', ['compose', 'stop', primary]);
    let status;
    for (let attempt = 0; attempt < 30; attempt++) {
        status = run(['status', others[0]])[0];
        if (status.members.some(member => member.state === 'PRIMARY')) break;
    }
    check('replacement primary elected', status.members.some(member => member.state === 'PRIMARY'));
    check('three acknowledgements unavailable with two members', run(['write', `${prefix}-all`, '3'], 1)[0].outcome === 'UnknownWriteOutcome');
    check('timed-out write is nevertheless present', run(['read', `${prefix}-all`, 'primary'])[0].outcome === 'Present');
    check('timed-out write verifies without retry', run(['verify', `${prefix}-all`])[0].outcome === 'PresentMatches');
    const replacement = status.members.find(member => member.state === 'PRIMARY').name.split(':')[0];
    command('docker', ['compose', 'stop', replacement]);
    check('no-primary verification is unknown, not absent', run(['verify', `${prefix}-majority`], 1)[0].outcome === 'UnknownReadFailure');
} finally { command('bash', ['run.sh', 'up']); }
check('verification succeeds after recovery', run(['verify', `${prefix}-majority`])[0].outcome === 'PresentMatches');
console.log(`${checks} live replication checks passed.`);