
const m = db.getSiblingDB("mobility");

const start = ISODate("2026-10-02T06:00:00Z");
const end = ISODate("2026-10-02T07:00:00Z");

const query = {
  cityId: "CPH",
  fromStopId: "STOP-NORREPORT",
  toStopId: "STOP-AIRPORT",
  "departures.status": "Scheduled",
  "departures.departureUtc": {
    $gte: start,
    $lt: end
  }
};

print("Results from the broken query:");
printjson(m.journey_search.find(query).toArray());
