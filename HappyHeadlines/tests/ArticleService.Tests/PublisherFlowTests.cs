using System.Net;
using System.Net.Http.Json;
using HappyHeadlines.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PublisherService;
using Xunit;

namespace ArticleService.Tests;

public sealed class PublisherFlowTests
{
    [Fact]
    public async Task Valid_article_is_queued_after_profanity_validation()
    {
        using var factory = new PublisherApiFactory();
        using var client = factory.CreateClient();
        var articleId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/publish", new { articleId, title = "Good", body = "News", continent = "Europe", isGlobal = false });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, factory.Publisher.Messages.Count);
        Assert.Equal(articleId, factory.Publisher.Messages[0].ArticleId);
    }

    [Fact]
    public async Task Profane_article_is_rejected_without_publishing()
    {
        using var factory = new PublisherApiFactory { Validation = new ArticleTextValidationResult(true, ["blocked"]) };
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/publish", new { articleId = Guid.NewGuid(), title = "bad", body = "body", continent = "Europe", isGlobal = false });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Publisher.Messages);
    }

    [Fact]
    public async Task Profanity_service_unavailable_fails_closed()
    {
        using var factory = new PublisherApiFactory { Failure = new HttpRequestException("unavailable") };
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/publish", new { articleId = Guid.NewGuid(), title = "valid-looking", body = "body", continent = "Europe", isGlobal = false });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(factory.Publisher.Messages);
    }
}

public sealed class PublisherApiFactory : WebApplicationFactory<PublisherServiceProgram>
{
    public FakeArticlePublisher Publisher { get; } = new();
    public ArticleTextValidationResult Validation { get; init; } = new(false, []);
    public Exception? Failure { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPublishedArticlePublisher>();
            services.RemoveAll<IArticleTextValidator>();
            services.RemoveAll<RabbitMqConnection>();
            services.AddSingleton<IPublishedArticlePublisher>(Publisher);
            services.AddSingleton<IArticleTextValidator>(new FakeArticleTextValidator(Validation, Failure));
        });
    }
}

public sealed class FakeArticlePublisher : IPublishedArticlePublisher
{
    public List<ArticlePublishedMessage> Messages { get; } = [];
    public Task PublishAsync(ArticlePublishedMessage message, CancellationToken cancellationToken) { Messages.Add(message); return Task.CompletedTask; }
}

public sealed class FakeArticleTextValidator(ArticleTextValidationResult result, Exception? failure) : IArticleTextValidator
{
    public Task<ArticleTextValidationResult> ValidateAsync(string text, CancellationToken cancellationToken) => failure is null ? Task.FromResult(result) : Task.FromException<ArticleTextValidationResult>(failure);
}
