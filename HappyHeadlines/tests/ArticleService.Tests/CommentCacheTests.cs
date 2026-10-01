using CommentService;
using HappyHeadlines.Observability;
using Xunit;

namespace ArticleService.Tests;

public sealed class CommentCacheTests
{
    [Fact]
    public async Task Miss_populates_cache_and_next_read_is_a_hit()
    {
        var articleId = Id(1);
        var comment = NewComment(articleId);
        var repository = new FakeCommentRepository();
        repository.Set(articleId, [comment]);
        var metrics = new CacheMetrics();
        var cache = new CommentCache(repository, metrics);

        Assert.Single(await cache.GetByArticleAsync(articleId, CancellationToken.None));
        Assert.Single(await cache.GetByArticleAsync(articleId, CancellationToken.None));

        Assert.Equal(1, repository.ReadCount(articleId));
        var stats = metrics.GetSnapshot(CommentCache.CacheName);
        Assert.Equal(1, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(0.5, stats.HitRatio);
    }

    [Fact]
    public async Task Evicts_least_recently_used_article_when_thirty_first_is_added()
    {
        var repository = new FakeCommentRepository();
        var cache = new CommentCache(repository, new CacheMetrics());
        var articleIds = Enumerable.Range(1, 31).Select(Id).ToArray();

        foreach (var articleId in articleIds.Take(30)) await cache.GetByArticleAsync(articleId, CancellationToken.None);
        await cache.GetByArticleAsync(articleIds[0], CancellationToken.None);
        await cache.GetByArticleAsync(articleIds[30], CancellationToken.None);
        await cache.GetByArticleAsync(articleIds[1], CancellationToken.None);

        Assert.Equal(2, repository.ReadCount(articleIds[1]));
        Assert.Equal(1, repository.ReadCount(articleIds[0]));
        Assert.Equal(1, repository.ReadCount(articleIds[30]));
    }

    [Fact]
    public async Task Invalidation_after_write_prevents_stale_comment_data()
    {
        var articleId = Id(42);
        var original = NewComment(articleId);
        var added = NewComment(articleId);
        var repository = new FakeCommentRepository();
        repository.Set(articleId, [original]);
        var cache = new CommentCache(repository, new CacheMetrics());

        Assert.Single(await cache.GetByArticleAsync(articleId, CancellationToken.None));
        await repository.CreateAsync(added, CancellationToken.None);
        cache.Invalidate(articleId);

        var reloaded = await cache.GetByArticleAsync(articleId, CancellationToken.None);
        Assert.Equal(2, reloaded.Count);
        Assert.Contains(reloaded, comment => comment.Id == added.Id);
    }

    private static Guid Id(int value) => new(value, 0, 0, new byte[8]);
    private static Comment NewComment(Guid articleId) => new(Guid.NewGuid(), articleId, "reader", "body", DateTimeOffset.UnixEpoch);

    private sealed class FakeCommentRepository : ICommentRepository
    {
        private readonly Dictionary<Guid, List<Comment>> _comments = [];
        private readonly Dictionary<Guid, int> _reads = [];
        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken)
        {
            if (!_comments.TryGetValue(comment.ArticleId, out var comments)) _comments[comment.ArticleId] = comments = [];
            comments.Add(comment);
            return Task.FromResult(comment);
        }
        public Task<IReadOnlyList<Comment>> GetByArticleAsync(Guid articleId, CancellationToken cancellationToken)
        {
            _reads[articleId] = ReadCount(articleId) + 1;
            return Task.FromResult<IReadOnlyList<Comment>>(_comments.TryGetValue(articleId, out var comments) ? comments.ToArray() : []);
        }
        public void Set(Guid articleId, List<Comment> comments) => _comments[articleId] = comments;
        public int ReadCount(Guid articleId) => _reads.TryGetValue(articleId, out var count) ? count : 0;
    }
}
