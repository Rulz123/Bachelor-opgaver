using HappyHeadlines.Observability;

namespace CommentService;

public sealed class CommentCache(ICommentRepository repository, CacheMetrics metrics)
{
    public const string CacheName = "comment_cache";
    public const int Capacity = 30;

    private readonly object _gate = new();
    private readonly Dictionary<Guid, CacheEntry> _entries = [];
    private readonly LinkedList<Guid> _leastToMostRecent = [];
    private long _generation;

    public async Task<IReadOnlyList<Comment>> GetByArticleAsync(Guid articleId, CancellationToken cancellationToken)
    {
        long generation;
        lock (_gate)
        {
            if (_entries.TryGetValue(articleId, out var cached))
            {
                Touch(cached);
                metrics.RecordHit(CacheName);
                return cached.Comments.ToArray();
            }

            generation = _generation;
        }

        metrics.RecordMiss(CacheName);
        var comments = await repository.GetByArticleAsync(articleId, cancellationToken);
        var copy = comments.ToArray();
        lock (_gate)
        {
            if (generation != _generation) return copy;
            if (_entries.TryGetValue(articleId, out var concurrentlyCached))
            {
                Touch(concurrentlyCached);
                return concurrentlyCached.Comments.ToArray();
            }

            var node = _leastToMostRecent.AddLast(articleId);
            _entries.Add(articleId, new CacheEntry(copy, node));
            if (_entries.Count > Capacity)
            {
                var leastRecent = _leastToMostRecent.First!;
                _leastToMostRecent.RemoveFirst();
                _entries.Remove(leastRecent.Value);
            }
        }

        return copy;
    }

    public void Invalidate(Guid articleId)
    {
        lock (_gate)
        {
            _generation++;
            if (_entries.Remove(articleId, out var entry))
                _leastToMostRecent.Remove(entry.Node);
        }
    }

    private void Touch(CacheEntry entry)
    {
        _leastToMostRecent.Remove(entry.Node);
        _leastToMostRecent.AddLast(entry.Node);
    }

    private sealed record CacheEntry(Comment[] Comments, LinkedListNode<Guid> Node);
}
