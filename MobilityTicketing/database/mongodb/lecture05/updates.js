const m = db.getSiblingDB("mobility");

function showCopies() {
  printjson(
    m.journey_search.aggregate([
      { $unwind: "$departures" },
      { $match: { "departures.tripId": "LAB05-T-OK" } },
      {
        $project: {
          _id: 1,
          toStopId: 1,
          status: "$departures.status"
        }
      },
      { $sort: { _id: 1 } }
    ]).toArray()
  );
}

function cancelTrip() {
  return m.journey_search.updateMany(
    {
      _id: /^LAB05:/,
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
  );
}

print("BEFORE:");
showCopies();

print("FIRST CANCELLATION:");
printjson(cancelTrip());
showCopies();

print("REPEATED CANCELLATION:");
printjson(cancelTrip());