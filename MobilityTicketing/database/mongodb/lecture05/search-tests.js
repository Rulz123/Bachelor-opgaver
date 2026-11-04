load("/scripts/lecture05/search.js");

function checkSearch(label, from, to, start, end, expectedIds) {
  const results = search(
    "CPH", from, to, ISODate(start), ISODate(end)
  );

  const actualIds = results.map(result => result.tripId);

  if (JSON.stringify(actualIds) !== JSON.stringify(expectedIds)) {
    throw new Error(
      `${label}: expected ${JSON.stringify(expectedIds)}, ` +
      `got ${JSON.stringify(actualIds)}`
    );
  }

  print(`PASS: ${label} -> ${JSON.stringify(actualIds)}`);
}

const origin = "STOP-NORREPORT";
const destination = "STOP-AIRPORT";

checkSearch(
  "Normal search", origin, destination,
  "2026-10-02T06:00:00Z", "2026-10-02T07:00:00Z",
  ["LAB05-T-OK"]
);

checkSearch(
  "End is exclusive", origin, destination,
  "2026-10-02T06:00:00Z", "2026-10-02T06:20:00Z",
  []
);

checkSearch(
  "Start is inclusive", origin, destination,
  "2026-10-02T06:20:00Z", "2026-10-02T07:00:00Z",
  ["LAB05-T-OK"]
);

checkSearch(
  "Reverse direction", destination, origin,
  "2026-10-02T06:00:00Z", "2026-10-02T07:00:00Z",
  ["LAB05-T-E"]
);

checkSearch(
  "Following day", origin, destination,
  "2026-10-03T06:00:00Z", "2026-10-03T07:00:00Z",
  ["LAB05-T-F"]
);

checkSearch(
  "Unknown destination", origin, "STOP-NO-MATCH",
  "2026-10-02T06:00:00Z", "2026-10-02T07:00:00Z",
  []
);

function checkRejected(label, action, expectedMessage) {
  try {
    action();
  } catch (error) {
    if (error.message !== expectedMessage) {
      throw error;
    }

    print(`PASS: ${label} rejected`);
    return;
  }

  throw new Error(`FAIL: ${label} was accepted`);
}

checkRejected(
  "End equals start",
  () => search(
    "CPH", origin, destination,
    ISODate("2026-10-02T06:00:00Z"),
    ISODate("2026-10-02T06:00:00Z")
  ),
  "Provide valid dates with end after start"
);

checkRejected(
  "End before start",
  () => search(
    "CPH", origin, destination,
    ISODate("2026-10-02T07:00:00Z"),
    ISODate("2026-10-02T06:00:00Z")
  ),
  "Provide valid dates with end after start"
);

checkRejected(
  "Empty stop ID",
  () => search(
    "CPH", "", destination,
    ISODate("2026-10-02T06:00:00Z"),
    ISODate("2026-10-02T07:00:00Z")
  ),
  "City and stop IDs must not be empty"
);