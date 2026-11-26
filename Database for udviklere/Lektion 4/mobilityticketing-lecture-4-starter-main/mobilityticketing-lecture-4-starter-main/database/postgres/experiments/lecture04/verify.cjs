const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const root = path.resolve(__dirname, '../../../..');
assert.match(process.env.COMPOSE_PROJECT_NAME || '', /^dbdverify-l04-/);
const database = `dbdverify_l04_${Date.now()}`;
function execute(args, input = '', code = 0) {
    const result = spawnSync('docker', ['compose', ...args], { cwd: root, input, encoding: 'utf8', timeout: 120000 });
    process.stdout.write(result.stdout || '');
    process.stderr.write(result.stderr || '');
    assert.equal(result.status, code, result.error?.message || result.stderr);
    return result.stdout.trim();
}
function sql(input, code = 0, variables = {}) {
    console.log('SQL:', input);
    return execute(['exec', '-T', 'postgres', 'psql', '-X', '-U', 'mobility', '-d', database, '-At', '-v', 'ON_ERROR_STOP=1',
        ...Object.entries(variables).flatMap(([name, value]) => ['-v', `${name}=${value}`])], input, code);
}
function file(name, code = 0, variables = {}) { return sql(fs.readFileSync(path.join(root, name), 'utf8'), code, variables); }
const migrations = 'database/postgres/migrations/';
const experiments = 'database/postgres/experiments/lecture04/';
let checks = 0;
function check(name, action) { action(); checks++; console.log('PASS', name); }
execute(['up', '-d', '--wait', 'postgres']);
console.log('CREATE ISOLATED DATABASE:', database);
execute(['exec', '-T', 'postgres', 'createdb', '-U', 'mobility', database]);
for (const name of fs.readdirSync(path.join(root, 'database/postgres/init')).filter(name => name.endsWith('.sql')).sort()) file(`database/postgres/init/${name}`);
const baseline = sql('select id,product_code,price,currency from tickets order by id;');
check('unsafe removal breaks old reader and rolls back', () => file(experiments + 'unsafe_change.sql.example', 3));
file(migrations + '030_expand_product_identity.sql.example');
check('new reader works before backfill', () => file(experiments + 'new_reader.sql.example'));
check('old writer works during expansion', () => file(experiments + 'old_writer.sql.example'));
check('requiring ID before backfill fails', () => file(migrations + '032_require_ticket_product.sql.example', 3));
const product = sql("select product_id from products where code='SINGLE';");
const variables = { ticket_id: 'LAB04-NEW', ticket_code: 'LAB04-CODE-NEW', product_id: product, purchase_price: '31', purchase_currency: 'DKK' };
check('new writer preserves agreed price', () => {
    file(experiments + 'new_writer.sql.example', 0, variables);
    assert.equal(sql("select price from tickets where id='LAB04-NEW';"), '31');
});
check('unknown product rejected', () => file(experiments + 'new_writer.sql.example', 3, { ...variables, ticket_id: 'LAB04-BAD', ticket_code: 'LAB04-BAD', product_id: '00000000-0000-0000-0000-000000000000' }));
check('mismatched code and ID rejected', () => sql("update tickets set product_id=(select product_id from products where code='DAY') where id='LAB04-NEW';", 3));
file(migrations + '031_backfill_ticket_product.sql.example');
check('backfill is idempotent', () => assert.equal(file(migrations + '031_backfill_ticket_product.sql.example'), 'UPDATE 0'));
file(experiments + 'old_writer.sql.example', 0, { ticket_id: 'LAB04-LATE', ticket_code: 'LAB04-CODE-LATE' });
check('late old write backfills exactly once', () => assert.equal(file(migrations + '031_backfill_ticket_product.sql.example'), 'UPDATE 1'));
check('reference verification has no invalid rows', () => assert.equal(file(experiments + 'verify.sql.example'), ''));
file(migrations + '032_require_ticket_product.sql.example');
check('retired old writer fails', () => file(experiments + 'old_writer.sql.example', 3, { ticket_id: 'LAB04-RETIRED', ticket_code: 'LAB04-CODE-RETIRED' }));
check('original identities and prices are unchanged', () => assert.equal(sql("select id,product_code,price,currency from tickets where id like 'TICKET-%' order by id;"), baseline));
file(experiments + 'remove_legacy.sql.example');
sql('alter table tickets drop column product_code;');
check('final writer and reader work without legacy column', () => {
    file(experiments + 'final_writer.sql.example');
    file(experiments + 'final_reader.sql.example');
    assert.equal(sql("select count(*) from tickets where id='LAB04-FINAL-1';"), '1');
});
console.log(`${checks} migration checks passed. Test database retained: ${database}`);