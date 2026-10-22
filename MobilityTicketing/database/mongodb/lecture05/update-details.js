load("/scripts/lecture05/search.js");

const collection = db.getSiblingDB("mobility").journey_search;

function showTrips(label, start, end) {
  print(label);

  printjson(search(
    "CPH",
    "STOP-NORREPORT",
    "STOP-AIRPORT",
    ISODate(start),
    ISODate(end)
  ));
}

// Move every copy of the trip.
print("MOVE DEPARTURE:");

printjson(collection.updateMany(
  {
    _id: /^LAB05:/,
    "departures.tripId": "LAB05-T-OK"
  },
  {
    $set: {
      "departures.$[trip].departureUtc":
        ISODate("2026-10-02T07:05:00Z"),
      "departures.$[trip].arrivalUtc":
        ISODate("2026-10-02T07:25:00Z")
    }
  },
  {
    arrayFilters: [{ "trip.tripId": "LAB05-T-OK" }]
  }
));

showTrips(
  "OLD WINDOW:",
  "2026-10-02T06:00:00Z",
  "2026-10-02T07:00:00Z"
);

showTrips(
  "NEW WINDOW:",
  "2026-10-02T07:00:00Z",
  "2026-10-02T08:00:00Z"
);