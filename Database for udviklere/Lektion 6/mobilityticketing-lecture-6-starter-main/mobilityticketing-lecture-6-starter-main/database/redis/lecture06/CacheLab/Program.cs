
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;

const int SchemaVersion = 2;

var cacheExpiry = TimeSpan.FromSeconds(20);

var jsonOptions = new JsonSerializerOptions(
    JsonSerializerDefaults.Web
);

// MongoDB
var source = new MongoJourneySearch();

// Redis: one shared connection
var options = ConfigurationOptions.Parse("127.0.0.1:6379");
options.AbortOnConnectFail = false;
options.ConnectTimeout = 500;
options.AsyncTimeout = 500;
options.ConnectRetry = 1;

ConnectionMultiplexer? redis = null;
IDatabase? cache = null;

try
{
    redis = await ConnectionMultiplexer.ConnectAsync(options);
    cache = redis.GetDatabase();
}
catch (Exception ex)
{
    Console.WriteLine($"cache unavailable: {ex.Message}");
}

// Search requests
var morning = new SearchRequest(
    "CPH",
    "STOP-NORREPORT",
    "STOP-AIRPORT",
    DateTimeOffset.Parse("2026-10-02T06:00:00Z"),
    DateTimeOffset.Parse("2026-10-02T07:00:00Z")
);

var evening = new SearchRequest(
    "CPH",
    "STOP-NORREPORT",
    "STOP-AIRPORT",
    DateTimeOffset.Parse("2026-10-02T18:00:00Z"),
    DateTimeOffset.Parse("2026-10-02T19:00:00Z")
);

// Cache key
string CreateKey(SearchRequest request)
{
    return string.Join(":",
        "journey",
        request.CityId,
        request.FromStopId,
        request.ToStopId,
        request.Start.ToUniversalTime().ToString("O"),
        request.End.ToUniversalTime().ToString("O")
    );
}

async Task SafeDeleteCacheKey(string key)
{
    if (cache == null)
        return;

    try
    {
        await cache.KeyDeleteAsync(key);
        Console.WriteLine($"Cache key deleted: {key}");
    }
    catch (RedisException ex)
    {
        Console.WriteLine($"cache unavailable: {ex.Message}");
    }
}

// Cached search
async Task RunSearch(SearchRequest request)
{
    var key = CreateKey(request);
    var stopwatch = Stopwatch.StartNew();

    var callsBefore = source.Calls;
    var dataSource = "MongoDB";

    List<Journey>? result = null;

    Console.WriteLine("\n======================================");
    Console.WriteLine($"Cache key: {key}");

    // Redis read
    if (cache != null)
    {
        try
        {
            var cached = await cache.StringGetAsync(key);

            if (!cached.IsNullOrEmpty)
            {
                try
                {
                    using var document =
                        JsonDocument.Parse(cached.ToString());

                    var root = document.RootElement;

                    JsonElement items = default;

                    var valid =
                        root.ValueKind == JsonValueKind.Object &&
                        root.TryGetProperty("schemaVersion", out var version) &&
                        version.TryGetInt32(out var schema) &&
                        schema == SchemaVersion &&
                        root.TryGetProperty("cachedAtUtc", out var timestamp) &&
                        timestamp.ValueKind == JsonValueKind.String &&
                        DateTimeOffset.TryParse(
                            timestamp.GetString(), out var cachedAt) &&
                        cachedAt.Offset == TimeSpan.Zero &&
                        root.TryGetProperty("items", out items) &&
                        items.ValueKind == JsonValueKind.Array;

                    if (valid)
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            if (!IsValidJourney(item))
                            {
                                valid = false;
                                break;
                            }
                        }
                    }

                    if (valid)
                    {
                        var envelope =
                            JsonSerializer.Deserialize<CacheEnvelope>(
                                cached.ToString(), jsonOptions);

                        if (envelope?.Items != null)
                        {
                            result = envelope.Items;
                            dataSource = "Redis";
                        }
                        else
                        {
                            Console.WriteLine("invalid");
                        }
                    }
                    else
                    {
                        Console.WriteLine("invalid");
                    }
                }
                catch (JsonException)
                {
                    Console.WriteLine("invalid");
                }
            }
            else
            {
                Console.WriteLine("cache MISS");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"cache unavailable: {ex.Message}");
        }
    }

    // MongoDB fallback
    if (result == null)
    {
        try
        {
            Console.WriteLine("Fetching from MongoDB...");

            result = await source.Fetch(request);
            dataSource = "MongoDB";

            var envelope = new CacheEnvelope(
                SchemaVersion,
                DateTimeOffset.UtcNow,
                result
            );

            var json = JsonSerializer.Serialize(
                envelope, jsonOptions);

            if (cache != null)
            {
                try
                {
                    await cache.StringSetAsync(
                        key,
                        json,
                        cacheExpiry
                    );

                    Console.WriteLine("cache STORED");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"cache write unavailable: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Console.WriteLine($"SEARCH ERROR: {ex.Message}");
            Console.WriteLine(
                $"MongoDB calls: {source.Calls - callsBefore}");
            Console.WriteLine(
                $"Elapsed: {stopwatch.ElapsedMilliseconds} ms");

            return;
        }
    }

    stopwatch.Stop();

    Console.WriteLine($"Data source: {dataSource}");
    Console.WriteLine($"Returned trips: {result.Count}");

    foreach (var journey in result)
    {
        Console.WriteLine($"Trip ID: {journey.TripId}");
    }

    Console.WriteLine(
        $"MongoDB calls: {source.Calls - callsBefore}");

    Console.WriteLine(
        $"Elapsed: {stopwatch.ElapsedMilliseconds} ms");

    Console.WriteLine("======================================");
}

// Journey validation
static bool IsValidJourney(JsonElement item)
{
    if (item.ValueKind != JsonValueKind.Object)
        return false;

    bool HasString(string name) =>
        item.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString());

    bool HasDate(string name, out DateTimeOffset date)
    {
        date = default;

        return item.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(value.GetString(), out date);
    }

    bool HasInteger(string name) =>
        item.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number) &&
        number >= 0;

    bool HasDecimal(string name) =>
        item.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number &&
        value.TryGetDecimal(out var number) &&
        number >= 0;

    return
        HasString("cityId") &&
        HasString("routeId") &&
        HasString("fromStopId") &&
        HasString("toStopId") &&
        HasString("tripId") &&
        HasDate("departureUtc", out var departure) &&
        HasDate("arrivalUtc", out var arrival) &&
        arrival >= departure &&
        HasInteger("availableSeats") &&
        HasDecimal("price") &&
        HasString("currency");
}

// Direct uncached MongoDB search
async Task<List<Journey>?> RunUncachedSearch(SearchRequest request)
{
    var stopwatch = Stopwatch.StartNew();
    var callsBefore = source.Calls;

    try
    {
        var result = await source.Fetch(request);

        stopwatch.Stop();

        Console.WriteLine("UNCACHED SEARCH");
        Console.WriteLine("Data source: MongoDB");
        Console.WriteLine($"Returned trips: {result.Count}");

        foreach (var journey in result)
            Console.WriteLine($"Trip ID: {journey.TripId}");

        Console.WriteLine(
            $"MongoDB calls: {source.Calls - callsBefore}");

        Console.WriteLine(
            $"Elapsed: {stopwatch.ElapsedMilliseconds} ms");

        return result;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        Console.WriteLine($"SEARCH ERROR: {ex.Message}");
        return null;
    }
}

// --------------------------------------------------
// TEST A: Repeat morning search
// --------------------------------------------------

Console.WriteLine("\n### TEST A: Repeat morning search");

var morningKey = CreateKey(morning);
Console.WriteLine($"MORNING KEY: {morningKey}");

if (cache != null)
    await SafeDeleteCacheKey(morningKey);

await RunSearch(morning);
await Task.Delay(1000);
await RunSearch(morning);

// --------------------------------------------------
// TEST B: Empty evening result
// --------------------------------------------------

Console.WriteLine("\n### TEST B: Empty evening result");

var eveningKey = CreateKey(evening);
Console.WriteLine($"EVENING KEY: {eveningKey}");

if (cache != null)
    await SafeDeleteCacheKey(eveningKey);

await RunSearch(evening);
await RunSearch(evening);

// --------------------------------------------------
// TEST C: Expire morning result
// --------------------------------------------------


Console.WriteLine("\n### TEST C: Expire morning result");

if (cache == null)
{
    Console.WriteLine("cache unavailable: Redis not connected.");
}
else
{
    try
    {
        // Set remaining TTL to one second
        var updated = await cache.KeyExpireAsync(
            morningKey,
            TimeSpan.FromSeconds(1)
        );

        if (!updated)
        {
            Console.WriteLine(
                "Cache key does not exist. Run Test A first.");
        }
        else
        {
            Console.WriteLine("Morning key expiry set to 1 second.");

            // Wait until Redis confirms expiration
            while (true)
            {
                var ttl = await cache.KeyTimeToLiveAsync(morningKey);

                if (ttl == null)
                    break;

                await Task.Delay(250);
            }

            Console.WriteLine("Morning key expired.");

            // MongoDB should be called again
            await RunSearch(morning);
        }
    }
    catch (RedisException ex)
    {
        Console.WriteLine($"cache unavailable: {ex.Message}");

        // Continue with MongoDB
        await RunSearch(morning);
    }
}


// --------------------------------------------------
// TEST D: Invalid cache values
// --------------------------------------------------


Console.WriteLine("\n### TEST D1: Invalid JSON");

if (cache == null)
{
    Console.WriteLine("cache unavailable: Redis not connected.");
}
else
{
    try
    {
        await cache.StringSetAsync(
            morningKey,
            "not-json",
            TimeSpan.FromSeconds(20)
        );

        Console.WriteLine("Invalid JSON stored.");

        await RunSearch(morning);
    }
    catch (RedisException ex)
    {
        Console.WriteLine($"cache unavailable: {ex.Message}");
    }
}


Console.WriteLine("\n### TEST D2: Wrong schema");

if (cache == null)
{
    Console.WriteLine("cache unavailable: Redis not connected.");
}
else
{
    try
    {
        var wrongSchema =
       """
        {
            "schemaVersion": 1,
            "cachedAtUtc": "2026-10-31T12:00:00Z",
            "items": []
        }
        """;

        await cache.StringSetAsync(
            morningKey,
            wrongSchema,
            TimeSpan.FromSeconds(20)
        );

        await RunSearch(morning);
    }
    catch (RedisException ex)
    {
        Console.WriteLine($"cache unavailable: {ex.Message}");
    }
}

Console.WriteLine("\n### TEST D3: Missing items");

if (cache == null)
{
    Console.WriteLine("cache unavailable: Redis not connected.");
}
else
{
    try
    {
        var missingItems =
        """
        {
            "schemaVersion": 2,
            "cachedAtUtc": "2026-10-31T12:00:00Z"
        }
        """;

        await cache.StringSetAsync(
            morningKey,
            missingItems,
            TimeSpan.FromSeconds(20)
        );

        await RunSearch(morning);
    }
    catch (RedisException ex)
    {
        Console.WriteLine($"cache unavailable: {ex.Message}");
    }
}

// --------------------------------------------------
// TEST 5: Cancel a cached departure
// --------------------------------------------------
Console.WriteLine("\n### TEST 5: Cancel a cached departure");

// 5.1 Cache morning search
await RunSearch(morning);

// 5.2 Cancel journey in MongoDB
Console.WriteLine("\nCancelling LAB05-T-OK...");

var cancelledCount = await source.CancelTrip("LAB05-T-OK");

Console.WriteLine($"Documents updated: {cancelledCount}");

// 5.3 Uncached search - should return []
await RunUncachedSearch(morning);

// 5.4 Cached search - can still return LAB05-T-OK
await RunSearch(morning);

// 5.5 Delete Redis cache
await SafeDeleteCacheKey(morningKey);

// 5.6 Cached search - should now return []
await RunSearch(morning);

// Restore original expiry for any subsequent searches
cacheExpiry = TimeSpan.FromSeconds(20);

Console.WriteLine("\nCache expiry restored to 20 seconds.");



// --------------------------------------------------
// Cache models
// --------------------------------------------------


// --------------------------------------------------
// TEST 6: Redis and MongoDB outages
// --------------------------------------------------

Console.WriteLine("\n### TEST 6A: Redis unavailable");

Console.WriteLine(
    "Stop Redis using: docker compose stop redis");

Console.WriteLine(
    "Press ENTER when Redis is stopped.");

Console.ReadLine();

// Redis fails, MongoDB should still work
await RunSearch(morning);


Console.WriteLine("\n### TEST 6B: Redis and MongoDB unavailable");

Console.WriteLine(
    "Stop MongoDB using: docker compose stop mongo");

Console.WriteLine(
    "Press ENTER when MongoDB is stopped.");

Console.ReadLine();

// Both services fail; search should report an error
await RunSearch(morning);


Console.WriteLine("\n### TEST 6C: Restore services");

Console.WriteLine(
    "Run: docker compose start mongo redis");

Console.WriteLine(
    "Wait until both services are ready, then press ENTER.");

Console.ReadLine();

// Both services should work again
await RunSearch(morning);


// --------------------------------------------------
// TEST 7: Concurrent searches
// --------------------------------------------------

Console.WriteLine("\n### TEST 7: Concurrent searches");

if (cache == null)
{
    Console.WriteLine("Redis unavailable. Cannot run cache concurrency test.");
}
else
{
    // Delete the exact morning key
    await SafeDeleteCacheKey(morningKey);

    // FIRST BATCH: Cache miss
    Console.WriteLine("\n7.1 First batch: 10 concurrent requests");

    var callsBeforeFirstBatch = source.Calls;
    var firstBatchTimer = Stopwatch.StartNew();

    await Task.WhenAll(
        Enumerable.Range(0, 10)
            .Select(_ => RunSearch(morning))
    );

    firstBatchTimer.Stop();

    var firstBatchCalls = source.Calls - callsBeforeFirstBatch;

    Console.WriteLine("\nFIRST BATCH SUMMARY");
    Console.WriteLine($"Requests: 10");
    Console.WriteLine($"MongoDB calls: {firstBatchCalls}");
    Console.WriteLine($"Elapsed: {firstBatchTimer.ElapsedMilliseconds} ms");

    // SECOND BATCH: Cache should be populated
    Console.WriteLine("\n7.2 Second batch: 10 concurrent requests");

    var callsBeforeSecondBatch = source.Calls;
    var secondBatchTimer = Stopwatch.StartNew();

    await Task.WhenAll(
        Enumerable.Range(0, 10)
            .Select(_ => RunSearch(morning))
    );

    secondBatchTimer.Stop();

    var secondBatchCalls = source.Calls - callsBeforeSecondBatch;

    Console.WriteLine("\nSECOND BATCH SUMMARY");
    Console.WriteLine($"Requests: 10");
    Console.WriteLine($"MongoDB calls: {secondBatchCalls}");
    Console.WriteLine($"Elapsed: {secondBatchTimer.ElapsedMilliseconds} ms");

    Console.WriteLine("\nCOMPARISON");
    Console.WriteLine($"First batch MongoDB calls: {firstBatchCalls}");
    Console.WriteLine($"Second batch MongoDB calls: {secondBatchCalls}");
}

//Test 8: Artificial delay in MongoDB docker commands
//docker compose exec -T redis redis-cli SET availability:LAB06:TRIP-ONE 1 EX 60
//docker compose exec -T redis redis-cli --eval /scripts/lecture06/decrement_if_positive.lua availability:LAB06:TRIP-ONE
//Expected : (integer) 0
//docker compose exec -T redis redis-cli --eval /scripts/lecture06/decrement_if_positive.lua availability:LAB06:TRIP-ONE
//Expected : (integer) -1
//docker compose exec -T redis redis-cli GET availability:LAB06:TRIP-ONE
//Expected : (integer) 0 
//Delete key
//docker compose exec -T redis redis-cli DEL availability:LAB06:TRIP-ONE
//docker compose exec -T redis redis-cli --eval /scripts/lecture06/decrement_if_positive.lua availability:LAB06:TRIP-ONE
//Expected : availability unknown

//Test 9: Decide what belong in redis
//docker compose exec -T redis redis-cli SET route:LAB09 "Copenhagen Airport Express" EX 86400
//read: docker compose exec -T redis redis-cli GET route:LAB09
//Expected : "Copenhagen Airport Express"
//delete: docker compose exec -T redis redis-cli DEL route:LAB09
//Expected : (integer) 1
//Store, read and delete a stop value
//docker compose exec -T redis redis-cli SET stop:STOP-NORREPORT "Nørreport Station" EX 86400
//read: docker compose exec -T redis redis-cli GET stop:STOP-NORREPORT
//Expected : "Nørreport Station"
//delete: docker compose exec -T redis redis-cli DEL stop:STOP-NORREPORT
//Expected : (integer) 1

// Close Redis
if (redis != null)
    await redis.DisposeAsync();

public sealed record CacheEnvelope(
    int SchemaVersion,
    DateTimeOffset CachedAtUtc,
    List<Journey> Items
);
