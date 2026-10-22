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

  return m.journey_search_by_trip.aggregate([
    {
      $match: {
        cityId,
        fromStopId,
        toStopId,
        status: "Scheduled",
        departureUtc: {
          $gte: start,
          $lt: end
        }
      }
    },
    {
      $sort: { departureUtc: 1 }
    },
    {
      $project: {
        _id: 0,
        cityId: 1,
        routeId: 1,
        fromStopId: 1,
        toStopId: 1,
        tripId: 1,
        departureUtc: 1,
        arrivalUtc: 1,
        availableSeats: 1,
        price: 1,
        currency: 1
      }
    }
  ]).toArray();
}

