const m = db.getSiblingDB("mobility");

const journeys = m.journey_search.find({
  _id: /^LAB05:/
}).toArray();

let processed = 0;

for (const journey of journeys) {
  for (const departure of journey.departures) {
    const identity = [
      journey.cityId,
      journey.routeId,
      journey.fromStopId,
      journey.toStopId,
      journey.serviceDate,
      departure.tripId
    ];

    const document = {
      _id: "LAB05:" + JSON.stringify(identity),
      cityId: journey.cityId,
      routeId: journey.routeId,
      fromStopId: journey.fromStopId,
      toStopId: journey.toStopId,
      serviceDate: journey.serviceDate,
      tripId: departure.tripId,
      departureUtc: departure.departureUtc,
      arrivalUtc: departure.arrivalUtc,
      status: departure.status,
      availableSeats: departure.availableSeats,
      price: journey.price,
      currency: journey.currency,
      schemaVersion: NumberInt(1)
    };

    m.journey_search_by_trip.replaceOne(
      { _id: document._id },
      document,
      { upsert: true }
    );

    processed++;
  }
}

printjson({
  processedDepartures: processed,
  storedDocuments: m.journey_search_by_trip.countDocuments({
    _id: /^LAB05:/
  }),
  copiesOfEligibleTrip: m.journey_search_by_trip.countDocuments({
    tripId: "LAB05-T-OK"
  })
});

print("EXAMPLE DOCUMENT:");
printjson(
  m.journey_search_by_trip.findOne({
    tripId: "LAB05-T-OK",
    toStopId: "STOP-AIRPORT"
  })
);