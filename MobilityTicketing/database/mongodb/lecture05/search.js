const m = db.getSiblingDB("mobility");

function search(cityId, fromStopId, toStopId, start, end) {
  const ids = [cityId, fromStopId, toStopId];

  if (ids.some(id => typeof id !== "string" || id.trim() === "")) {
    throw new Error("City and stop IDs must not be empty");
  }

  if (
    !(start instanceof Date) ||
    !(end instanceof Date) ||
    Number.isNaN(start.getTime()) ||
    Number.isNaN(end.getTime()) ||
    end <= start
  ) {
    throw new Error("Provide valid dates with end after start");
  }

  return m.journey_search.aggregate([
    {
      $match: { cityId, fromStopId, toStopId }
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
      $sort: { "departures.departureUtc": 1 }
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
    }
  ]).toArray();
}

