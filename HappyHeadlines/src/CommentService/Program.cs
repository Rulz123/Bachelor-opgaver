using CommentService;
using HappyHeadlines.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddHappyHeadlinesObservability("comment-service");
builder.Services.AddSingleton<ICommentRepository, PostgresCommentRepository>();
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<CommentCache>();
builder.Services.AddTransient<TraceContextHandler>();
builder.Services.AddSingleton(new CircuitBreaker(new CircuitBreakerOptions
{
    FailureThreshold = builder.Configuration.GetValue("PROFANITY_FAILURE_THRESHOLD", 3),
    BreakDuration = TimeSpan.FromSeconds(builder.Configuration.GetValue("PROFANITY_BREAK_SECONDS", 5))
}));
builder.Services.AddHttpClient<IProfanityClient, HttpProfanityClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PROFANITY_SERVICE_URL"] ?? "http://profanity-service:8080");
}).AddHttpMessageHandler<TraceContextHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseHappyHeadlinesObservability("comment-service");
var repository = app.Services.GetRequiredService<ICommentRepository>();
if (!app.Environment.IsEnvironment("Testing")) await repository.InitializeAsync(app.Lifetime.ApplicationStopping);

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", (IConfiguration configuration, CircuitBreaker breaker) => Results.Json(new
{
    status = breaker.State == CircuitState.Closed ? "healthy" : "not_ready",
    instance = configuration["INSTANCE_NAME"] ?? Environment.MachineName,
    profanityCircuit = breaker.State.ToString()
}, statusCode: breaker.State == CircuitState.Closed ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable));

app.MapGet("/comments", async (Guid articleId, CommentCache cache, CancellationToken cancellationToken) =>
    Results.Ok(await cache.GetByArticleAsync(articleId, cancellationToken)));

app.MapGet("/cache/metrics", (CacheMetrics metrics) => Results.Ok(metrics.GetSnapshot(CommentCache.CacheName)));

app.MapPost("/comments", async (CreateCommentRequest request, ICommentRepository store, CommentCache cache, IProfanityClient profanity, CircuitBreaker breaker, CancellationToken cancellationToken) =>
{
    if (request.ArticleId == Guid.Empty || string.IsNullOrWhiteSpace(request.Author) || string.IsNullOrWhiteSpace(request.Body))
        return Results.BadRequest("ArticleId, author and body are required.");
    try
    {
        var validation = await breaker.ExecuteAsync(ct => profanity.ValidateAsync(request.Body, ct), cancellationToken);
        if (validation.IsProfane) return Results.BadRequest(new ValidationFailure("profanity_detected", "Comment contains blocked language."));
        var comment = new Comment(Guid.NewGuid(), request.ArticleId, request.Author, request.Body, DateTimeOffset.UtcNow);
        await store.CreateAsync(comment, cancellationToken);
        cache.Invalidate(request.ArticleId);
        return Results.Created($"/comments/{comment.Id}?articleId={comment.ArticleId}", comment);
    }
    catch (CircuitOpenException exception) { return Results.Json(new ValidationFailure("profanity_unavailable", exception.Message), statusCode: StatusCodes.Status503ServiceUnavailable); }
    catch (ProfanityUnavailableException exception) { return Results.Json(new ValidationFailure("profanity_unavailable", exception.Message), statusCode: StatusCodes.Status503ServiceUnavailable); }
});

app.Run();

public partial class CommentServiceProgram { }
