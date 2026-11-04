load("/scripts/lecture05/search-alternative.js");

const collection = db.getSiblingDB("mobility").journey_search_by_trip;

function showSearches(label) {
  print(label);

  for (const destination of ["STOP-AIRPORT", "STOP-CENTRAL"]) {
    const results = search(
      "CPH",
      "STOP-NORREPORT",
      destination,
      ISODate("2026-10-02T06:00:00Z"),
      ISODate("2026-10-02T07:00:00Z")
    );

    printjson({
      destination,
      trips: results.map(result => result.tripId)
    });
  }
}

showSearches("BEFORE CANCELLATION:");

printjson(collection.updateMany(
  {
    _id: /^LAB05:/,
    tripId: "LAB05-T-OK"
  },
  {
    $set: { status: "Cancelled" }
  }
));

showSearches("AFTER CANCELLATION:");