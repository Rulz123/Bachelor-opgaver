const m = db.getSiblingDB("mobility");

const start = ISODate("2026-10-02T06:00:00Z");
const end = ISODate("2026-10-02T07:00:00Z");

function search(cityId, fromStopId, toStopId, start, end) {
  return m.journey_search.aggregate([
    {
      $match: {
        cityId,
        fromStopId,
        toStopId,
        departures: {
          $elemMatch: {
            departureUtc: {
              $gte: start,
              $lte: end
            },
            status: "Scheduled"
          }
        }
      }
    }
  ]).toArray();
}

printjson(search("CPH", "STOP-NORREPORT", "STOP-AIRPORT", start, end));

print("All documents: " +
  m.journey_search.countDocuments({}));

print("City only: " +
  m.journey_search.countDocuments({ cityId: "CPH" }));

print("City + origin: " +
  m.journey_search.countDocuments({
    cityId: "CPH",
    fromStopId: "STOP-NORREPORT"
  }));

print("City + origin + destination: " +
  m.journey_search.countDocuments({
    cityId: "CPH",
    fromStopId: "STOP-NORREPORT",
    toStopId: "STOP-AIRPORT"
  }));

print("All filters + time: " +
  m.journey_search.countDocuments({
    cityId: "CPH",
    fromStopId: "STOP-NORREPORT",
    toStopId: "STOP-AIRPORT",
    departures: {
      $elemMatch: {
        departureUtc: { $gte: start, $lte: end },
        status: "Scheduled"
      }
    }
  }));

