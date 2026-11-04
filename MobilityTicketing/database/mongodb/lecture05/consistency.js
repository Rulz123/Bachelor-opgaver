load("/scripts/lecture05/search.js");

function showPassengerResults(label) {
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

showPassengerResults("BEFORE — both searches show the trip");

// Deliberately update only document A.
printjson(
  db.getSiblingDB("mobility").journey_search.updateOne(
    { _id: "LAB05:A" },
    {
      $set: {
        "departures.$[trip].status": "Cancelled"
      }
    },
    {
      arrayFilters: [{ "trip.tripId": "LAB05-T-OK" }]
    }
  )
);

showPassengerResults("INCONSISTENT — only Airport hides the trip");

// Repair every copy using the same cancellation.
printjson(
  db.getSiblingDB("mobility").journey_search.updateMany(
    {
      _id: /^LAB05:/,
      "departures.tripId": "LAB05-T-OK"
    },
    {
      $set: {
        "departures.$[trip].status": "Cancelled"
      }
    },
    {
      arrayFilters: [{ "trip.tripId": "LAB05-T-OK" }]
    }
  )
);

showPassengerResults("REPAIRED — both searches hide the trip");