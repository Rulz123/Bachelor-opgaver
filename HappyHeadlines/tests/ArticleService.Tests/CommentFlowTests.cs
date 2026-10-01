using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using CommentService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ArticleService.Tests;

public class CommentFlowTests : IClassFixture<CommentApiFactory>
{
    private readonly HttpClient _client;

    public CommentFlowTests(CommentApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Complete_comment_flow_validates_and_persists_comment()
    {
        var articleId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/comments", new { articleId, author = "reader", body = "A kind comment" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comments = await _client.GetFromJsonAsync<List<Comment>>("/comments?articleId=" + articleId);
        Assert.Single(comments!);
    }

    [Fact]
    public async Task Profane_comment_is_rejected_and_not_persisted()
    {
        var articleId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/comments", new { articleId, author = "reader", body = "This is spam" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await _client.GetFromJsonAsync<List<Comment>>("/comments?articleId=" + articleId) ?? []);
    }

    [Fact]
    public async Task Comment_write_invalidates_previously_cached_miss()
    {
        var articleId = Guid.NewGuid();
        Assert.Empty(await _client.GetFromJsonAsync<List<Comment>>("/comments?articleId=" + articleId) ?? []);
        var response = await _client.PostAsJsonAsync("/comments", new { articleId, author = "reader", body = "A newly written comment" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var comments = await _client.GetFromJsonAsync<List<Comment>>("/comments?articleId=" + articleId);
        Assert.Single(comments!);
        Assert.Equal("A newly written comment", comments[0].Body);
    }

    [Fact]
    public async Task Unavailable_profanity_service_fails_closed()
    {
        var factory = new CommentApiFactory();
        factory.Profanity.Failure = new ProfanityUnavailableException("down");
        using var client = factory.CreateClient();
        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            response = await client.PostAsJsonAsync("/comments", new { articleId = Guid.NewGuid(), author = "reader", body = "Should not be stored" });
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
        var failure = await response!.Content.ReadFromJsonAsync<ValidationFailure>();
        Assert.Equal("profanity_unavailable", failure!.Error);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health")).StatusCode);
    }
}

public sealed class CommentApiFactory : WebApplicationFactory<CommentServiceProgram>
{
    public FakeProfanityClient Profanity { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICommentRepository>();
            services.RemoveAll<IProfanityClient>();
            services.RemoveAll<CircuitBreaker>();
            services.AddSingleton<ICommentRepository, InMemoryCommentRepository>();
            services.AddSingleton<IProfanityClient>(Profanity);
            services.AddSingleton(new CircuitBreaker(new CircuitBreakerOptions { FailureThreshold = 3, BreakDuration = TimeSpan.FromSeconds(1) }));
        });
    }
}

public sealed class FakeProfanityClient : IProfanityClient
{
    public Exception? Failure { get; set; }
    public Task<ProfanityResult> ValidateAsync(string text, CancellationToken cancellationToken)
    {
        if (Failure is not null) throw Failure;
        var matches = text.Contains("spam", StringComparison.OrdinalIgnoreCase) ? new[] { "spam" } : Array.Empty<string>();
        return Task.FromResult(new ProfanityResult(matches.Length > 0, matches));
    }
}

public sealed class InMemoryCommentRepository : ICommentRepository
{
    private readonly ConcurrentDictionary<Guid, Comment> _comments = new();
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken) { _comments[comment.Id] = comment; return Task.FromResult(comment); }
    public Task<IReadOnlyList<Comment>> GetByArticleAsync(Guid articleId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Comment>>(_comments.Values.Where(comment => comment.ArticleId == articleId).ToList());
}
