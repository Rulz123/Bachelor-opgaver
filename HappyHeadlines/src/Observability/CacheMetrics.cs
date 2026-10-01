using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace HappyHeadlines.Observability;

public sealed record CacheMetricsSnapshot(string CacheName, long Hits, long Misses, double HitRatio);

public sealed class CacheMetrics : IDisposable
{
    public const string MeterName = "HappyHeadlines.Caches";
    private const string CacheTag = "cache_name";
    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly ConcurrentDictionary<string, CacheState> _states = new(StringComparer.Ordinal);
    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;

    public CacheMetrics()
    {
        _hits = _meter.CreateCounter<long>("happy_headlines_cache_hits", description: "Cumulative cache hits.");
        _misses = _meter.CreateCounter<long>("happy_headlines_cache_misses", description: "Cumulative cache misses.");
        _meter.CreateObservableGauge("happy_headlines_cache_hit_ratio", ObserveRatios, unit: "1", description: "Cumulative hits divided by total cache accesses; zero before the first access.");
    }

    public void RecordHit(string cacheName)
    {
        var state = GetState(cacheName);
        Interlocked.Increment(ref state.Hits);
        _hits.Add(1, new KeyValuePair<string, object?>(CacheTag, cacheName));
    }

    public void RecordMiss(string cacheName)
    {
        var state = GetState(cacheName);
        Interlocked.Increment(ref state.Misses);
        _misses.Add(1, new KeyValuePair<string, object?>(CacheTag, cacheName));
    }

    public CacheMetricsSnapshot GetSnapshot(string cacheName)
    {
        var state = GetState(cacheName);
        var hits = Interlocked.Read(ref state.Hits);
        var misses = Interlocked.Read(ref state.Misses);
        var total = hits + misses;
        return new CacheMetricsSnapshot(cacheName, hits, misses, total == 0 ? 0 : (double)hits / total);
    }

    private CacheState GetState(string cacheName) => _states.GetOrAdd(cacheName, static _ => new CacheState());

    private IEnumerable<Measurement<double>> ObserveRatios()
    {
        foreach (var (cacheName, state) in _states)
        {
            var hits = Interlocked.Read(ref state.Hits);
            var misses = Interlocked.Read(ref state.Misses);
            var total = hits + misses;
            var tags = new[] { new KeyValuePair<string, object?>(CacheTag, cacheName) };
            yield return new Measurement<double>(total == 0 ? 0 : (double)hits / total, tags);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private sealed class CacheState
    {
        public long Hits;
        public long Misses;
    }
}
