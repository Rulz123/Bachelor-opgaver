// Compare only a successfully read document; transport errors are handled by the runner.
function compareChange(actual, expected) {
  if (actual === null) return 'AbsentAtVerification';
  return ['_id', 'tripId', 'status', 'sourceVersion'].every(key => actual[key] === expected[key])
    ? 'PresentMatches' : 'PresentConflicts';
}
