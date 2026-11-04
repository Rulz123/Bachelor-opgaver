const m = db.getSiblingDB("mobility");

const original = m.journey_search.findOne({
  _id: "LAB05:A"
});

function measureGrowth(count) {
  const document = {
    ...original,
    _id: "LAB05:GROWTH",
    departures: Array.from({ length: count }, (_, i) => ({
      ...original.departures[0],
      tripId: `LAB05-GROWTH-${i}`
    }))
  };

  m.journey_search.replaceOne(
    { _id: document._id },
    document,
    { upsert: true }
  );

  return m.journey_search.aggregate([
    { $match: { _id: "LAB05:GROWTH" } },
    {
      $project: {
        _id: 0,
        departures: { $size: "$departures" },
        bytes: { $bsonSize: "$$ROOT" }
      }
    }
  ]).toArray()[0];
}

const small = measureGrowth(100);
const large = measureGrowth(1000);

print("100 DEPARTURES:");
printjson(small);

print("1,000 DEPARTURES:");
printjson(large);

print(`Size increased by ${(large.bytes / small.bytes).toFixed(2)} times`);