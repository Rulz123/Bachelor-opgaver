const m = db.getSiblingDB('mobility');

printjson(
  m.journey_search.updateOne(
    {
      _id: "LAB05:A",
      "departures.tripId": "LAB05-T-OK"
    },
    {
      $set: {
        "departures.$[trip].status": "Cancelled"
      }
    },
    {
      arrayFilters: [
        { "trip.tripId": "LAB05-T-OK" }
      ]
    }
  )
);

const start = ISODate("2026-10-02T06:00:00Z");
const end = ISODate("2026-10-02T07:00:00Z");

print("Nørreport to Airport:");
printjson(
  search("CPH", "STOP-NORREPORT", "STOP-AIRPORT", start, end)
);

print("Nørreport to Central:");
printjson(
  search("CPH", "STOP-NORREPORT", "STOP-CENTRAL", start, end)
);