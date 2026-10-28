
const m = db.getSiblingDB("mobility");

const journeys = m.journey_search.find({
  _id: /^LAB05:[A-F]$/
}).toArray();

const documents = [];

for (const journey of journeys) {
  for (const departure of journey.departures) {
    documents.push({
      _id: [
        journey.cityId,
        journey.routeId,
        journey.fromStopId,
        journey.toStopId,
        departure.tripId
      ].join("|"),
      cityId: journey.cityId,
      routeId: journey.routeId,
      fromStopId: journey.fromStopId,
      toStopId: journey.toStopId,
      tripId: departure.tripId,
      departureUtc: departure.departureUtc,
      arrivalUtc: departure.arrivalUtc,
      status: departure.status,
      availableSeats: departure.availableSeats,
      price: journey.price,
      currency: journey.currency,
      schemaVersion: journey.schemaVersion
    });
  }
}

m.journey_search_by_trip.drop();

if (documents.length > 0) {
  m.journey_search_by_trip.insertMany(documents);
}

print("Source journeys: " + journeys.length);
print("Documents prepared: " + documents.length);
print("Documents stored: " +
  m.journey_search_by_trip.countDocuments());
