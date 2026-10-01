using ArticleService;
using HappyHeadlines.Observability;
using HappyHeadlines.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.AddHappyHeadlinesObservability("article-service");
builder.Services.AddSingleton<IArticleRepository, PostgresArticleRepository>();
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IArticleCache, ArticleCache>();
builder.Services.AddSingleton<RabbitMqConnection>();
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<ArticleQueueHostedService>();
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<ArticleCacheRefreshService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseHappyHeadlinesObservability("article-service");
var repository = app.Services.GetRequiredService<IArticleRepository>();
if (!app.Environment.IsEnvironment("Testing"))
{
    await repository.InitializeAsync(app.Lifetime.ApplicationStopping);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", (IConfiguration configuration) => Results.Ok(new
{
    status = "healthy",
    instance = configuration["INSTANCE_NAME"] ?? Environment.MachineName
}));

app.MapGet("/articles", async (IArticleRepository store, CancellationToken cancellationToken) =>
    Results.Ok(await store.GetAllAsync(cancellationToken)));

app.MapGet("/articles/recent", async (IArticleCache cache, CancellationToken cancellationToken) =>
    Results.Ok(await cache.GetRecentAsync(cancellationToken)));

app.MapGet("/cache/metrics", (CacheMetrics metrics) => Results.Ok(metrics.GetSnapshot(ArticleCache.CacheName)));

app.MapGet("/articles/{id:guid}", async (Guid id, IArticleCache cache, CancellationToken cancellationToken) =>
{
    var article = await cache.GetAsync(id, cancellationToken);
    return article is null ? Results.NotFound() : Results.Ok(article);
});

app.MapPost("/articles", async (CreateArticleRequest request, IArticleRepository store, IArticleCache cache, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body)) return Results.BadRequest("Title and body are required.");
    try
    {
        ArticleRouter.GetDatabaseKey(request.IsGlobal, request.Continent);
        var now = DateTimeOffset.UtcNow;
        var article = new Article(Guid.NewGuid(), request.Title, request.Body, request.Continent, request.IsGlobal, now, now);
        await store.CreateAsync(article, cancellationToken);
        cache.Upsert(article);
        return Results.Created($"/articles/{article.Id}", article);
    }
    catch (ArgumentException exception) { return Results.BadRequest(exception.Message); }
});

app.MapPut("/articles/{id:guid}", async (Guid id, UpdateArticleRequest request, IArticleRepository store, IArticleCache cache, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body)) return Results.BadRequest("Title and body are required.");
    try
    {
        ArticleRouter.GetDatabaseKey(request.IsGlobal, request.Continent);
        var existing = await store.GetAsync(id, cancellationToken);
        if (existing is null) return Results.NotFound();
        var article = existing with { Title = request.Title, Body = request.Body, Continent = request.Continent, IsGlobal = request.IsGlobal, UpdatedAt = DateTimeOffset.UtcNow };
        if (!await store.UpdateAsync(article, cancellationToken)) return Results.NotFound();
        cache.Upsert(article);
        return Results.Ok(article);
    }
    catch (ArgumentException exception) { return Results.BadRequest(exception.Message); }
});

app.MapDelete("/articles/{id:guid}", async (Guid id, IArticleRepository store, IArticleCache cache, CancellationToken cancellationToken) =>
{
    if (!await store.DeleteAsync(id, cancellationToken)) return Results.NotFound();
    cache.Remove(id);
    return Results.NoContent();
});

app.Run();

public partial class Program { }
