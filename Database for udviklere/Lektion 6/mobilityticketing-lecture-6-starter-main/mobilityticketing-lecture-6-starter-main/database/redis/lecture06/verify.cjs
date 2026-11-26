const assert = require('node:assert/strict');
const { execFileSync, spawn } = require('node:child_process');
const path = require('node:path');

const cwd = path.resolve(__dirname, '../../..');
const project = process.env.COMPOSE_PROJECT_NAME;
assert.match(project || '', /^dbdverify-l06-/, 'Use an isolated dbdverify-l06-* Compose project');
function compose(...args) {
    console.log('COMMAND: docker compose -p', project, ...args);
    const result = execFileSync('docker', ['compose', '-p', project, ...args], {
        cwd, encoding: 'utf8', maxBuffer: 8 * 1024 * 1024, timeout: 120000
    });
    process.stdout.write(result);
    return result.trim();
}
function redis(...args) { return compose('exec', '-T', 'redis', 'redis-cli', ...args); }

async function main() {
    compose('up', '-d', '--wait', 'mongo', 'redis');
    assert.equal(compose('port', 'mongo', '27017'), '127.0.0.1:27017');
    assert.equal(compose('port', 'redis', '6379'), '127.0.0.1:6379');
    compose('exec', '-T', 'mongo', 'mongosh', '--quiet', '--file', '/scripts/lecture05/reset.js');
    console.log('COMMAND: dotnet build database/redis/lecture06/CacheLab');
    process.stdout.write(execFileSync('dotnet', ['build', 'database/redis/lecture06/CacheLab'], {
        cwd, encoding: 'utf8', maxBuffer: 8 * 1024 * 1024
    }));
    let output = '';
    const handled = new Set();
    console.log('COMMAND: dotnet run --no-build --project database/redis/lecture06/CacheLab');
    try {
        await new Promise((resolve, reject) => {
            const child = spawn('dotnet', ['run', '--no-build', '--project', 'database/redis/lecture06/CacheLab'], { cwd });
            const deadline = setTimeout(() => {
                child.kill();
                reject(new Error('Cache smoke run exceeded 180 seconds'));
            }, 180000);
            child.stderr.on('data', chunk => process.stderr.write(chunk));
            child.stdout.on('data', chunk => {
                output += chunk.toString();
                process.stdout.write(chunk);
                try {
                    for (const [prompt, command] of [
                        ['Press ENTER when Redis is stopped.', ['stop', 'redis']],
                        ['Press ENTER when MongoDB is stopped.', ['stop', 'mongo']],
                        ['Wait until both services are ready, then press ENTER.', ['up', '-d', '--wait', 'mongo', 'redis']]
                    ]) {
                        if (output.includes(prompt) && !handled.has(prompt)) {
                            handled.add(prompt);
                            compose(...command);
                            child.stdin.write('\n');
                        }
                    }
                } catch (error) { child.kill(); reject(error); }
            });
            child.on('error', reject);
            child.on('close', code => {
                clearTimeout(deadline);
                code === 0 ? resolve() : reject(new Error(`Cache program exited ${code}`));
            });
        });
    } finally { compose('up', '-d', '--wait', 'mongo', 'redis'); }
    let checks = 0;
    function check(name, action) { action(); checks++; console.log('PASS', name); }
    function section(start, end) {
        const from = output.indexOf(`### TEST ${start}:`);
        const to = output.indexOf(`### TEST ${end}:`, from + 1);
        assert.ok(from >= 0 && to > from, `Missing section ${start}`);
        return output.slice(from, to);
    }
    check('cold MongoDB and warm Redis return the expected trip', () => {
        const text = section('A', 'B');
        assert.match(text, /Data source: MongoDB/);
        assert.match(text, /Data source: Redis/);
        assert.equal((text.match(/Trip ID: LAB05-T-OK/g) || []).length, 2);
    });
    check('empty results are cached', () => {
        const text = section('B', 'C');
        assert.match(text, /Data source: Redis\s+Returned trips: 0/);
    });
    check('expired entries are fetched again', () => assert.match(section('C', 'D1'), /Morning key expired\.[\s\S]*Data source: MongoDB/));
    for (const [start, end] of [['D1', 'D2'], ['D2', 'D3'], ['D3', '5']]) {
        check(`invalid cache ${start} falls back`, () => assert.match(section(start, end), /invalid[\s\S]*Data source: MongoDB/));
    }
    check('cancellation updates MongoDB and invalidation removes the stale result', () => {
        const text = section('5', '6A');
        assert.match(text, /Documents updated: 2/);
        assert.match(text, /UNCACHED SEARCH\s+Data source: MongoDB\s+Returned trips: 0/);
        assert.match(text, /UNCACHED SEARCH[\s\S]*Data source: Redis\s+Returned trips: 1[\s\S]*Data source: MongoDB\s+Returned trips: 0/);
    });
    check('Redis outage falls back to MongoDB', () => assert.match(section('6A', '6B'), /Data source: MongoDB/));
    check('both services unavailable is an error, not an empty result', () => {
        const text = section('6B', '6C');
        assert.match(text, /SEARCH ERROR:/);
        assert.doesNotMatch(text, /Returned trips:/);
    });
    check('services recover successfully', () => assert.match(section('6C', '7'), /Data source: (MongoDB|Redis)/));
    check('warm concurrent batch makes no MongoDB calls', () => assert.match(output, /Second batch MongoDB calls: 0/));
    const key = 'availability:LAB06:VERIFY';
    redis('SET', key, '1', 'EX', '60');
    check('Lua decrements the last seat', () => assert.equal(redis('--eval', '/scripts/lecture06/decrement_if_positive.lua', key), '0'));
    check('Lua cannot oversell', () => assert.equal(redis('--eval', '/scripts/lecture06/decrement_if_positive.lua', key), '-1'));
    redis('DEL', key);
    check('Lua treats a missing key as unknown', () => assert.match(redis('--eval', '/scripts/lecture06/decrement_if_positive.lua', key), /availability unknown/));
    console.log(`${checks} live cache checks passed.`);
}
main().catch(error => { console.error(error); process.exitCode = 1; });