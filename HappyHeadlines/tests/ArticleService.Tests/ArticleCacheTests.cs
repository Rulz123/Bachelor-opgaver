using ArticleService;
using HappyHeadlines.Observability;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ArticleService.Tests;

public sealed class ArticleCacheTests
{
    [Fact]
    public void Refresh_interval_is_configurable()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ARTICLE_CACHE_REFRESH_INTERVAL_SECONDS"] = "45"
        }).Build();

        Assert.Equal(TimeSpan.FromSeconds(45), ArticleCacheRefreshService.GetRefreshInterval(configuration));
    }

    [Fact]
    public async Task Refresh_includes_only_articles_in_the_latest_fourteen_days_including_cutoff()
    {
        var now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeArticleRepository();
        repository.Articles =
        [
            ArticleAt(now.AddDays(-14)),
            ArticleAt(now.AddDays(-14).AddTicks(-1)),
            ArticleAt(now.AddDays(-1)),
            ArticleAt(now.AddTicks(1))
        ];
        var cache = new ArticleCache(repository, new CacheMetrics(), new ManualTimeProvider(now));

        await cache.RefreshAsync(CancellationToken.None);
        var recent = await cache.GetRecentAsync(CancellationToken.None);

        Assert.Equal(2, recent.Count);
        Assert.Contains(recent, article => article.CreatedAt == now.AddDays(-14));
        Assert.Contains(recent, article => article.CreatedAt == now.AddDays(-1));
    }

    [Fact]
    public async Task Refresh_replaces_snapshot_but_failed_refresh_keeps_last_good_data_usable()
    {
        var now = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var original = ArticleAt(now.AddDays(-1));
        var replacement = ArticleAt(now);
        var repository = new FakeArticleRepository { Articles = [original] };
        var cache = new ArticleCache(repository, new CacheMetrics(), new ManualTimeProvider(now));

        await cache.RefreshAsync(CancellationToken.None);
        repository.FailReads = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.RefreshAsync(CancellationToken.None));
        Assert.Equal(original.Id, Assert.Single(await cache.GetRecentAsync(CancellationToken.None)).Id);

        repository.FailReads = false;
        repository.Articles = [replacement];
        await cache.RefreshAsync(CancellationToken.None);
        Assert.Equal(replacement.Id, Assert.Single(await cache.GetRecentAsync(CancellationToken.None)).Id);
    }

    private static Article ArticleAt(DateTimeOffset createdAt) => new(Guid.NewGuid(), "Title", "Body", "Europe", false, createdAt, createdAt);

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeArticleRepository : IArticleRepository
    {
        public IReadOnlyList<Article> Articles { get; set; } = [];
        public bool FailReads { get; set; }
        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken cancellationToken) => FailReads ? Task.FromException<IReadOnlyList<Article>>(new InvalidOperationException("read failure")) : Task.FromResult(Articles);
        public Task<Article?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Articles.FirstOrDefault(article => article.Id == id));
        public Task<Article> CreateAsync(Article article, CancellationToken cancellationToken) => Task.FromResult(article);
        public Task<bool> UpdateAsync(Article article, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
