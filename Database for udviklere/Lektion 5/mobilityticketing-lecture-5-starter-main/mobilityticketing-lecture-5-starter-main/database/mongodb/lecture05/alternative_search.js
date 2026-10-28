
const m = db.getSiblingDB("mobility");

function searchByTrip(cityId, fromStopId, toStopId, start, end) {
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

  return m.journey_search_by_trip.find({
    cityId: cityId,
    fromStopId: fromStopId,
    toStopId: toStopId,
    status: "Scheduled",
    departureUtc: {
      $gte: start,
      $lt: end
    }
  }, {
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
  }).sort({ departureUtc: 1 }).toArray();
}

const start = ISODate("2026-10-02T06:00:00Z");
const end = ISODate("2026-10-02T07:00:00Z");

printjson(
  searchByTrip(
    "CPH",
    "STOP-NORREPORT",
    "STOP-AIRPORT",
    start,
    end
  )
);
