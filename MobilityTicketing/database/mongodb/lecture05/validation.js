const m = db.getSiblingDB("mobility");

function expectValidationError(label, id, change) {
  const document = m.journey_search.findOne({ _id: "LAB05:A" });

  document._id = id;
  change(document);

  try {
    m.journey_search.insertOne(document);
  } catch (error) {
    if (error.code !== 121) {
      throw error;
    }

    print(`PASS: ${label} rejected with error ${error.code}`);
    return;
  }

  throw new Error(`FAIL: ${label} was accepted`);
}

expectValidationError(
  "Departure stored as a string",
  "LAB05:INVALID-DATE",
  document => {
    document.departures[0].departureUtc = "2026-10-02T06:20:00Z";
  }
);

expectValidationError(
  "Negative available seats",
  "LAB05:INVALID-SEATS",
  document => {
    document.departures[0].availableSeats = NumberInt(-1);
  }
);
