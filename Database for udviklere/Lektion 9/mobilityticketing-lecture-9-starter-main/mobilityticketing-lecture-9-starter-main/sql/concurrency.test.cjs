const assert = require('node:assert/strict');
const { spawn, execFileSync } = require('node:child_process');
const path = require('node:path');
const cwd = path.resolve(__dirname, '..');
assert.match(process.env.COMPOSE_PROJECT_NAME || '', /^dbdverify-l09-/);
const prefix = `live-${Date.now()}`;
const args = ['compose', 'exec', '-T', 'postgres', 'psql', '-X', '-U', 'mobility', '-d', 'mobility_l09', '-Atq', '-v', 'ON_ERROR_STOP=1'];
function sql(statement) {
    console.log('SQL:', statement);
    return execFileSync('docker', args, { cwd, input: statement, encoding: 'utf8', timeout: 60000 }).trim();
}
function concurrent(statement) {
    console.log('CONCURRENT SQL:', statement);
    return new Promise((resolve, reject) => {
        const child = spawn('docker', args, { cwd });
        let output = '', error = '';
        const deadline = setTimeout(() => { child.kill(); reject(new Error('SQL timed out')); }, 60000);
        child.stdout.on('data', chunk => output += chunk);
        child.stderr.on('data', chunk => error += chunk);
        child.on('error', reject);
        child.on('close', code => {
            clearTimeout(deadline);
            console.log(JSON.stringify({ code, output: output.trim(), error: error.trim() }));
            resolve({ code, output: output.trim(), error });
        });
        child.stdin.end(statement);
    });
}
async function main() {
    for (const [name, capacity] of [['same', 1], ['capacity', 2], ['left', 1], ['right', 1], ['rollback', 1]]) {
        sql(`insert into lab09.trips(id,capacity,reserved_seats,status) values('${prefix}-${name}',${capacity},0,'Scheduled');`);
    }
    let checks = 0;
    function check(name, action) { action(); checks++; console.log('PASS', name); }
    const same = await Promise.all(Array.from({ length: 10 }, () => concurrent(`select lab09.book('${prefix}-same','passenger','${prefix}-same');`)));
    check('ten identical requests return one original booking', () => {
        assert.ok(same.every(result => result.code === 0));
        assert.equal(new Set(same.map(result => result.output)).size, 1);
        assert.equal(sql(`select reserved_seats from lab09.trips where id='${prefix}-same';`), '1');
    });
    const capacity = await Promise.all(Array.from({ length: 20 }, (_, index) => concurrent(`select lab09.book('${prefix}-capacity-${index}','passenger-${index}','${prefix}-capacity');`)));
    check('twenty requests cannot oversell two seats', () => {
        assert.equal(capacity.filter(result => result.code === 0).length, 2);
        assert.ok(capacity.filter(result => result.code !== 0).every(result => result.code === 3 && result.error.includes('is full')));
    });
    const conflict = await Promise.all(['left', 'right'].map(trip => concurrent(`select lab09.book('${prefix}-conflict','passenger','${prefix}-${trip}');`)));
    check('concurrent request reuse for different trips conflicts', () => {
        assert.equal(conflict.filter(result => result.code === 0).length, 1);
        assert.equal(conflict.filter(result => result.code === 3 && result.error.includes('another booking')).length, 1);
    });
    sql(`begin; select lab09.book('${prefix}-rollback','passenger','${prefix}-rollback'); rollback;`);
    check('caller rollback removes booking and seat', () => {
        assert.equal(sql(`select reserved_seats from lab09.trips where id='${prefix}-rollback';`), '0');
        assert.equal(sql(`select count(*) from lab09.bookings where request_id='${prefix}-rollback';`), '0');
    });
    sql(`update lab09.trips set status='Cancelled' where id='${prefix}-same';`);
    check('retry after cancellation retains original booking', () => assert.equal(sql(`select lab09.book('${prefix}-same','passenger','${prefix}-same');`), same[0].output));
    check('all counters match persisted booking rows', () => assert.equal(sql(`select count(*) from lab09.trips trip where trip.id like '${prefix}%' and trip.reserved_seats <> (select count(*) from lab09.bookings booking where booking.trip_id=trip.id);`), '0'));
    console.log(`${checks} concurrent booking checks passed.`);
}
main().catch(error => { console.error(error); process.exitCode = 1; });