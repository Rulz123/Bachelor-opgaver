
const m = db.getSiblingDB("mobility");

printjson(
  m.journey_search.updateMany(
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
      arrayFilters: [
        { "trip.tripId": "LAB05-T-OK" }
      ]
    }
  )
);
