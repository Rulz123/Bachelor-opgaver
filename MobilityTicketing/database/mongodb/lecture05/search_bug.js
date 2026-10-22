const m = db.getSiblingDB("mobility");

const start = ISODate("2026-10-02T06:00:00Z");
const end = ISODate("2026-10-02T07:00:00Z");

const brokenQuery = {
  cityId: "CPH",
  fromStopId: "STOP-NORREPORT",
  toStopId: "STOP-AIRPORT",
  "departures.status": "Scheduled",
  "departures.departureUtc": {
    $gte: start,
    $lt: end
  }
};

print("BROKEN QUERY — matching document IDs:");
printjson(
  m.journey_search.find(brokenQuery).toArray().map(doc => doc._id)
);

print("DOCUMENT B — inspect its departures:");
printjson(
  m.journey_search.findOne({ _id: "LAB05:B" }).departures
);

const fixedQuery = {
  cityId: "CPH",
  fromStopId: "STOP-NORREPORT",
  toStopId: "STOP-AIRPORT",
  departures: {
    $elemMatch: {
      status: "Scheduled",
      departureUtc: {
        $gte: start,
        $lt: end
      }
    }
  }
};

print("FIXED QUERY — matching document IDs:");
printjson(
  m.journey_search.find(fixedQuery).toArray().map(doc => doc._id)
);