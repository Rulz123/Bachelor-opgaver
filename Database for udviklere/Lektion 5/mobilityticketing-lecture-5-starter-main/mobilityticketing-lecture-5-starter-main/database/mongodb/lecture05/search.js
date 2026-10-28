const m = db.getSiblingDB("mobility");

function search(cityId, fromStopId, toStopId, start, end) {
  
  if (typeof fromStopId !== "string" || !fromStopId.trim()) {
    throw new Error("fromStopId must not be empty");
  }

  if (typeof toStopId !== "string" || !toStopId.trim()) {
    throw new Error("toStopId must not be empty");
  }

  if (!(start instanceof Date) || !(end instanceof Date)) {
    throw new Error("start and end must be valid dates");
  }

  if (end <= start) {
    throw new Error("end must be later than start");
  }

  return m.journey_search.aggregate([
    {
      $match: {
        cityId: cityId,
        fromStopId: fromStopId,
        toStopId: toStopId,
        departures: {
          $elemMatch: {
            status: "Scheduled",
            departureUtc: { $gte: start, $lt: end }
          }
        }
      }
    },
    {
      $unwind: "$departures"
    },
    {
      $match: {
        "departures.status": "Scheduled",
        "departures.departureUtc": {
          $gte: start,
          $lt: end
        }
      }
    },
    {
      $project: {
        _id: 0,
        cityId: 1,
        routeId: 1,
        fromStopId: 1,
        toStopId: 1,
        tripId: "$departures.tripId",
        departureUtc: "$departures.departureUtc",
        arrivalUtc: "$departures.arrivalUtc",
        availableSeats: "$departures.availableSeats",
        price: 1,
        currency: 1
      }
    },
    {
      $sort: { departureUtc: 1 }
    }
  ]).toArray();
}

// Initial example search
const start = ISODate("2026-10-02T06:00:00Z");
const end = ISODate("2026-10-02T07:00:00Z");

printjson(
  search("CPH", "STOP-NORREPORT", "STOP-AIRPORT", start, end)
);