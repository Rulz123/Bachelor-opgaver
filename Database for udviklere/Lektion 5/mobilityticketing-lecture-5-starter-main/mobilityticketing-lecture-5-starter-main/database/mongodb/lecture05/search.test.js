
load("/scripts/lecture05/search.js");

const day2Start = ISODate("2026-10-02T06:00:00Z");
const day2End = ISODate("2026-10-02T07:00:00Z");
const boundary = ISODate("2026-10-02T06:20:00Z");
const day3Start = ISODate("2026-10-03T06:00:00Z");
const day3End = ISODate("2026-10-03T07:00:00Z");

let failures = 0;

function check(name, expected, actual) {
  const passed =
    JSON.stringify(expected) === JSON.stringify(actual);

  print((passed ? "PASS: " : "FAIL: ") + name);

  if (!passed) {
    print("Expected:");
    printjson(expected);
    print("Actual:");
    printjson(actual);
    failures++;
  }
}

function ids(from, to, start, end) {
  return search("CPH", from, to, start, end)
    .map(doc => doc.tripId);
}

function rejects(name, callback) {
  let rejected = false;

  try {
    callback();
  } catch (e) {
    rejected = true;
  }

  check(name, true, rejected);
}

// Required search cases
check(
  "Nørreport to Airport, 06:00-07:00",
  ["LAB05-T-OK"],
  ids("STOP-NORREPORT", "STOP-AIRPORT", day2Start, day2End)
);

check(
  "End at 06:20 is exclusive",
  [],
  ids("STOP-NORREPORT", "STOP-AIRPORT", day2Start, boundary)
);

check(
  "Start at 06:20 is inclusive",
  ["LAB05-T-OK"],
  ids("STOP-NORREPORT", "STOP-AIRPORT", boundary, day2End)
);

check(
  "Airport to Nørreport",
  ["LAB05-T-E"],
  ids("STOP-AIRPORT", "STOP-NORREPORT", day2Start, day2End)
);

check(
  "Same search on 3 October",
  ["LAB05-T-F"],
  ids("STOP-NORREPORT", "STOP-AIRPORT", day3Start, day3End)
);

check(
  "No matching destination",
  [],
  ids("STOP-NORREPORT", "STOP-NO-MATCH", day2Start, day2End)
);

// Invalid input cases
rejects("Reject end equal to start", () =>
  search("CPH", "STOP-NORREPORT", "STOP-AIRPORT",
    day2Start, day2Start)
);

rejects("Reject end before start", () =>
  search("CPH", "STOP-NORREPORT", "STOP-AIRPORT",
    day2End, day2Start)
);

rejects("Reject empty origin", () =>
  search("CPH", "", "STOP-AIRPORT", day2Start, day2End)
);

rejects("Reject empty destination", () =>
  search("CPH", "STOP-NORREPORT", " ", day2Start, day2End)
);

if (failures > 0) {
  print("FAILED: " + failures + " test(s)");
  quit(1);
}

print("All tests passed.");
