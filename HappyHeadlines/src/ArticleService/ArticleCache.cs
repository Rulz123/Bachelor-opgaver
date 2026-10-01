using HappyHeadlines.Observability;

namespace ArticleService;

public interface IArticleCache
{
    Task<IReadOnlyList<Article>> GetRecentAsync(CancellationToken cancellationToken);
    Task<Article?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task RefreshAsync(CancellationToken cancellationToken);
    void Upsert(Article article);
    void Remove(Guid id);
}

public sealed class ArticleCache(
    IArticleRepository repository,
    CacheMetrics metrics,
    TimeProvider timeProvider) : IArticleCache
{
    public const string CacheName = "article_cache";
    public static readonly TimeSpan PrefillWindow = TimeSpan.FromDays(14);

    private readonly object _gate = new();
    private Article[]? _recentArticles;
    private long _generation;

    public async Task<IReadOnlyList<Article>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var snapshot = Volatile.Read(ref _recentArticles);
        if (snapshot is null)
        {
            metrics.RecordMiss(CacheName);
            while (snapshot is null)
            {
                await RefreshAsync(cancellationToken);
                snapshot = Volatile.Read(ref _recentArticles);
            }
        }
        else
        {
            metrics.RecordHit(CacheName);
        }

        return snapshot.ToArray();
    }

    public async Task<Article?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var snapshot = Volatile.Read(ref _recentArticles);
        var cached = snapshot?.FirstOrDefault(article => article.Id == id);
        if (cached is not null)
        {
            metrics.RecordHit(CacheName);
            return cached;
        }

        metrics.RecordMiss(CacheName);
        var article = await repository.GetAsync(id, cancellationToken);
        if (article is not null) Upsert(article);
        return article;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        long generation;
        lock (_gate) generation = _generation;

        var articles = await repository.GetAllAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var cutoff = now - PrefillWindow;
        var recent = articles
            .Where(article => article.CreatedAt >= cutoff && article.CreatedAt <= now)
            .OrderByDescending(article => article.CreatedAt)
            .ToArray();

        lock (_gate)
        {
            if (generation != _generation) return;
            Volatile.Write(ref _recentArticles, recent);
        }
    }

    public void Upsert(Article article)
    {
        lock (_gate)
        {
            _generation++;
            var current = _recentArticles;
            if (current is null) return;

            var now = timeProvider.GetUtcNow();
            var cutoff = now - PrefillWindow;
            var retained = current.Where(existing => existing.Id != article.Id && existing.CreatedAt >= cutoff && existing.CreatedAt <= now).ToList();
            if (article.CreatedAt >= cutoff && article.CreatedAt <= now) retained.Add(article);
            Volatile.Write(ref _recentArticles, retained.OrderByDescending(existing => existing.CreatedAt).ToArray());
        }
    }

    public void Remove(Guid id)
    {
        lock (_gate)
        {
            _generation++;
            if (_recentArticles is { } current)
                Volatile.Write(ref _recentArticles, current.Where(article => article.Id != id).ToArray());
        }
    }
}

public sealed class ArticleCacheRefreshService(
    IArticleCache cache,
    IConfiguration configuration,
    ILogger<ArticleCacheRefreshService> logger) : BackgroundService
{
    public static TimeSpan GetRefreshInterval(IConfiguration configuration) =>
        TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("ARTICLE_CACHE_REFRESH_INTERVAL_SECONDS", 300)));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = GetRefreshInterval(configuration);
        await RefreshSafelyAsync(stoppingToken);
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RefreshSafelyAsync(stoppingToken);
    }

    private async Task RefreshSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await cache.RefreshAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "article_cache_refresh_failed; retaining last successful snapshot");
        }
    }
}
