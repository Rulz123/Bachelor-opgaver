const m = db.getSiblingDB("mobility");
const invalid = m.journey_search.findOne({ _id: "LAB05:A" });

invalid._id = "LAB05:INVALID";
invalid.departures[0].availableSeats = -1;
invalid.departures[0].departureTime = "2023-01-01T10:00:00Z";

m.journey_search.insertOne(invalid)

if(!invalid){
    print("No invalid document found");
}

if (invalid.error) {
    print(invalid.error);
}

