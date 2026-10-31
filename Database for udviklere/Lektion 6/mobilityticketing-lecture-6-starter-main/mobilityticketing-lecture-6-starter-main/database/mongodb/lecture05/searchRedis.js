const m = db.getSiblingDB("mobility");

const morningStart = ISODate("2026-10-02T06:00:00Z");
const morningEnd = ISODate("2026-10-02T07:00:00Z");

const eveningStart = ISODate("2026-10-02T18:00:00Z");
const eveningEnd = ISODate("2026-10-02T19:00:00Z");

function search(cityId, fromStopId, toStopId, start, end) {
  return m.journey_search.aggregate([
    {
      $match: {
        cityId,
        fromStopId,
        toStopId,
        departures: {
          $elemMatch: {
            departureUtc: {
              $gte: start,
              $lte: end
            },
            status: "Scheduled"
          }
        }
      }
    }
  ]).toArray();
}

const morning = search(
  "CPH",
  "STOP-NORREPORT",
  "STOP-AIRPORT",
  morningStart,
  morningEnd
);

const evening = search(
  "CPH",
  "STOP-NORREPORT",
  "STOP-AIRPORT",
  eveningStart,
  eveningEnd
);

print("Incomplete cache key:");
print("search:LAB06:CPH:STOP-NORREPORT:STOP-AIRPORT");

print("\nUncached morning result:");
printjson(morning);

print("\nUncached evening result:");
printjson(evening);


function part(value) {
  if (typeof value !== "string" || value.length === 0) {
    throw new Error("IDs must not be empty");
  }

  return encodeURIComponent(value);
}

function instant(value) {
  if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
    throw new Error("Invalid timestamp");
  }

  return value.toISOString();
}

function buildSearchKey(city, origin, destination, start, end) {
  if (end <= start) {
    throw new Error("End time must be after start time");
  }

  return [
    "search",
    "LAB06",
    "v2",
    part(city),
    part(origin),
    part(destination),
    part(instant(start)),
    part(instant(end))
  ].join(":");
}

const key = buildSearchKey(
  "CPH",
  "STOP-NORREPORT",
  "STOP-AIRPORT",
  morningStart,
  morningEnd
);

print("Complete cache key:");
print(key);


//docker compose exec redis redis-cli SET "search:LAB06:CPH:STOP-NORREPORT:STOP-AIRPORT" '[{"tripId":"LAB05-T-OK"}]' EX 300

//docker compose exec redis redis-cli GET "search:LAB06:CPH:STOP-NORREPORT:STOP-AIRPORT"

//docker compose exec redis redis-cli TTL "search:LAB06:CPH:STOP-NORREPORT:STOP-AIRPORT"

//docker compose exec redis redis-cli DEL "search:LAB06:CPH:STOP-NORREPORT:STOP-AIRPORT"