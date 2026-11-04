const collection = db.getSiblingDB("mobility").journey_search;

print("PRICE UPDATE:");

printjson(collection.updateMany(
  {
    _id: /^LAB05:/,
    cityId: "CPH",
    routeId: "LINE-M2"
  },
  {
    $set: {
      price: NumberDecimal("40.00")
    }
  }
));

print("PRICES AFTER UPDATE:");

printjson(
  collection.find(
    { _id: /^LAB05:/ },
    { _id: 1, cityId: 1, routeId: 1, price: 1 }
  ).sort({ _id: 1 }).toArray()
);