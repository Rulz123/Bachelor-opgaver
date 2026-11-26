const assert = require('node:assert/strict');
const { spawnSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
assert.match(process.env.COMPOSE_PROJECT_NAME || '', /^dbdverify-l11-/);
const work = fs.mkdtempSync(path.join(os.tmpdir(), 'dbd-l11-receipts-'));
console.log('Isolated receipts:', work);
const prefix = `live-${Date.now()}`;
const original = `${prefix}-original`;
const correction = `${prefix}-correction`;
const bucket = 'mobility-archive-lab';
const key = 'validation-exports/2026-11-20/validations.jsonl';
let checks = 0;
function command(program, args, code = 0, cwd = root) {
    console.log('COMMAND:', program, ...args);
    const result = spawnSync(program, args, { cwd, encoding: 'utf8', timeout: 180000, maxBuffer: 8 * 1024 * 1024 });
    process.stdout.write(result.stdout || '');
    process.stderr.write(result.stderr || '');
    assert.equal(result.status, code, result.error?.message || result.stderr);
    return result.stdout.trim();
}
function compose(...args) { return command('docker', ['compose', ...args]); }
function s3(...args) { return JSON.parse(compose('run', '--rm', '-T', 'aws', 's3api', ...args) || '{}'); }
function app(args, code = 0) {
    return command('dotnet', [path.join(root, 'src/MobilityTicketing.Reports/bin/Debug/net8.0/MobilityTicketing.Reports.dll'), ...args], code, work);
}
function sql(query) { return compose('exec', '-T', 'postgres', 'psql', '-X', '-U', 'mobility', '-d', 'mobility_l11', '-v', 'ON_ERROR_STOP=1', '-Atc', query); }
function check(name, action) { action(); checks++; console.log('PASS', name); }
function versions() { return s3('list-object-versions', '--bucket', bucket, '--prefix', key).Versions || []; }
try {
    compose('up', '-d', '--wait', 'postgres', 'object-store');
    assert.equal(compose('port', 'postgres', '5432'), '127.0.0.1:25411');
    assert.equal(compose('port', 'object-store', '9000'), '127.0.0.1:19011');
    const buckets = s3('list-buckets').Buckets;
    if (!buckets.some(item => item.Name === bucket)) s3('create-bucket', '--bucket', bucket);
    s3('put-bucket-versioning', '--bucket', bucket, '--versioning-configuration', 'Status=Enabled');
    check('bucket versioning enabled', () => assert.equal(s3('get-bucket-versioning', '--bucket', bucket).Status, 'Enabled'));
    command('dotnet', ['build', 'src/MobilityTicketing.Reports']);
    command('dotnet', ['run', '--project', 'tests/WorkflowChecks']);
    check('CLI starts', () => assert.match(app(['--help']), /Usage:/));
    check('publish original', () => assert.equal(app(['publish', original, path.join(root, 'fixtures/validations-v1.jsonl')]), `Saved ${original}`));
    check('original bytes verify', () => assert.equal(app(['verify', original]), 'Matches'));
    app(['stage', correction, path.join(root, 'fixtures/validations-v2.jsonl')]);
    const receiptPath = path.join(work, 'receipts', `${correction}.json`);
    const receipt = JSON.parse(fs.readFileSync(receiptPath, 'utf8'));
    const beforeVersions = versions().length;
    check('staged correction is not published', () => {
        assert.equal(app(['verify', correction], 3), 'UnknownReport');
        assert.equal(sql(`SELECT count(*) FROM reports WHERE report_id = '${correction}'`), '0');
    });
    check('mismatched receipt cannot publish', () => {
        fs.writeFileSync(receiptPath, JSON.stringify({ ...receipt, Sha256: '0'.repeat(64) }));
        app(['recover', correction, original], 1);
        assert.equal(sql(`SELECT count(*) FROM reports WHERE report_id = '${correction}'`), '0');
        fs.writeFileSync(receiptPath, JSON.stringify(receipt));
    });
    check('missing predecessor rolls back publication', () => {
        app(['recover', correction, `${prefix}-missing`], 1);
        assert.equal(sql(`SELECT count(*) FROM reports WHERE report_id = '${correction}'`), '0');
    });
    check('recovery is idempotent without reupload', () => {
        app(['recover', correction, original]);
        app(['recover', correction, original]);
        assert.equal(versions().length, beforeVersions);
        assert.equal(sql(`SELECT count(*) FROM reports WHERE report_id = '${correction}'`), '1');
    });
    check('changed predecessor conflicts', () => app(['recover', correction], 1));
    check('both exact versions verify after correction', () => {
        assert.equal(app(['verify', original]), 'Matches');
        assert.equal(app(['verify', correction]), 'Matches');
    });
    check('duplicate staging does not upload', () => {
        app(['stage', correction, path.join(root, 'fixtures/validations-v2.jsonl')], 1);
        assert.equal(versions().length, beforeVersions);
    });
    compose('stop', 'object-store');
    check('object outage is ReadFailed', () => assert.equal(app(['verify', original], 5), 'ReadFailed'));
    compose('up', '-d', '--wait', 'object-store');
    s3('list-buckets');
    compose('stop', 'postgres');
    check('catalogue outage is CatalogueReadFailed', () => assert.equal(app(['verify', original], 4), 'CatalogueReadFailed'));
} finally { compose('up', '-d', '--wait', 'postgres', 'object-store'); }
check('verification recovers after outages', () => assert.equal(app(['verify', original]), 'Matches'));
sql(`SELECT report_id, row_count, supersedes, version_id, sha256 FROM reports WHERE report_id LIKE '${prefix}%' ORDER BY report_id`);
console.log(`${checks} live report checks passed.`);