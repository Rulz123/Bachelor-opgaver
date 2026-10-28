
const m = db.getSiblingDB("mobility");

const original = m.journey_search.findOne({
  _id: "LAB05:A",
});

if (!original) {
  throw new Error("Document LAB05:A not found. Run setup.js first.");
}

function measureGrowth(count) {
  const growth = {
    ...original,
    _id: "LAB05:GROWTH",
    departures: Array.from({ length: count }, (_, i) => ({
      ...original.departures[0],
      tripId: `LAB05-GROWTH-${i}`,
    })),
  };

  // Replace the previous test document if it exists.
  m.journey_search.deleteOne({ _id: "LAB05:GROWTH" });
  m.journey_search.insertOne(growth);

  const result = m.journey_search.aggregate([
    { $match: { _id: "LAB05:GROWTH" } },
    {
      $project: {
        _id: 0,
        bytes: { $bsonSize: "$$ROOT" },
        departures: { $size: "$departures" },
      },
    },
  ]).toArray();

  printjson(result);
}

print("100 departures:");
measureGrowth(100);

print("1000 departures:");
measureGrowth(1000);


// As the number of departures grows, the BSON document becomes larger. 
// To prevent indefinite growth, 
// I would split journey-search documents by service date or use a bounded time window, 
// keeping each document to a manageable number of departures.