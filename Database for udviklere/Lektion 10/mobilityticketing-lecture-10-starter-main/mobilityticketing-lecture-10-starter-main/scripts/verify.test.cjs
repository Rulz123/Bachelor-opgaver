const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const context = vm.createContext({});
vm.runInContext(fs.readFileSync(__dirname + '/verify.js', 'utf8'), context);
const expected = { _id: 'retry-1', tripId: 'T1', status: 'Cancelled', sourceVersion: 2 };
assert.equal(context.compareChange(null, expected), 'AbsentAtVerification');
assert.equal(context.compareChange({...expected}, expected), 'PresentMatches');
for (const key of Object.keys(expected)) {
  assert.equal(context.compareChange({...expected, [key]: 'different'}, expected), 'PresentConflicts');
  const missing = {...expected}; delete missing[key];
  assert.equal(context.compareChange(missing, expected), 'PresentConflicts');
}
assert.equal(context.compareChange({...expected, sourceVersion: '2'}, expected), 'PresentConflicts');
assert.deepEqual(expected, { _id: 'retry-1', tripId: 'T1', status: 'Cancelled', sourceVersion: 2 });
console.log('12 comparison checks passed (absence, match, four conflicts, four missing fields, type, no mutation).');
