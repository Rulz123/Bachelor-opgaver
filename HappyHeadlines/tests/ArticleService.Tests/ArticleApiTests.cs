using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using ArticleService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ArticleService.Tests;

public class ArticleApiTests : IClassFixture<ArticleApiFactory>
{
    private readonly HttpClient _client;

    public ArticleApiTests(ArticleApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Supports_crud_for_continent_article()
    {
        var createdResponse = await _client.PostAsJsonAsync("/articles", new { title = "Europe", body = "Story", continent = "Europe", isGlobal = false });
        var created = await createdResponse.Content.ReadFromJsonAsync<Article>();
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/articles/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/articles/{created.Id}", new { title = "Updated", body = "Edited", continent = "Europe", isGlobal = false });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/articles/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/articles/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Stores_continent_and_global_articles_separately()
    {
        await _client.PostAsJsonAsync("/articles", new { title = "Europe", body = "Local", continent = "Europe", isGlobal = false });
        await _client.PostAsJsonAsync("/articles", new { title = "Global", body = "Everywhere", continent = (string?)null, isGlobal = true });

        var articles = await _client.GetFromJsonAsync<List<Article>>("/articles");
        Assert.NotNull(articles);
        Assert.Contains(articles!, article => article.Title == "Europe" && article.Continent == "Europe" && !article.IsGlobal);
        Assert.Contains(articles!, article => article.Title == "Global" && article.Continent is null && article.IsGlobal);
    }

    [Fact]
    public async Task Recent_endpoint_uses_article_cache_and_exposes_hit_ratio()
    {
        using var factory = new ArticleApiFactory();
        using var client = factory.CreateClient();
        var createdResponse = await client.PostAsJsonAsync("/articles", new { title = "Cached", body = "Recent story", continent = "Europe", isGlobal = false });
        var created = await createdResponse.Content.ReadFromJsonAsync<Article>();
        Assert.NotNull(created);

        var firstRead = await client.GetFromJsonAsync<List<Article>>("/articles/recent");
        var secondRead = await client.GetFromJsonAsync<List<Article>>("/articles/recent");
        var metrics = await client.GetFromJsonAsync<HappyHeadlines.Observability.CacheMetricsSnapshot>("/cache/metrics");

        Assert.Contains(firstRead!, article => article.Id == created!.Id);
        Assert.Contains(secondRead!, article => article.Id == created!.Id);
        Assert.Equal(1, metrics!.Hits);
        Assert.Equal(1, metrics.Misses);
        Assert.Equal(0.5, metrics.HitRatio);
    }
}

public sealed class ArticleApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IArticleRepository>();
            services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
        });
    }
}

public sealed class InMemoryArticleRepository : IArticleRepository
{
    private readonly ConcurrentDictionary<Guid, Article> _articles = new();

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<IReadOnlyList<Article>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Article>>(_articles.Values.OrderByDescending(article => article.CreatedAt).ToList());
    public Task<Article?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_articles.GetValueOrDefault(id));
    public Task<Article> CreateAsync(Article article, CancellationToken cancellationToken) { _articles[article.Id] = article; return Task.FromResult(article); }
    public Task<bool> UpdateAsync(Article article, CancellationToken cancellationToken) { _articles[article.Id] = article; return Task.FromResult(true); }
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_articles.TryRemove(id, out _));
}
