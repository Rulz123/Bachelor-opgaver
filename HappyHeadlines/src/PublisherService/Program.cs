using System.Diagnostics;
using HappyHeadlines.Messaging;
using HappyHeadlines.Observability;
using PublisherService;

var builder = WebApplication.CreateBuilder(args);
builder.AddHappyHeadlinesObservability("publisher-service");
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<IPublishedArticlePublisher, ArticleQueuePublisher>();
builder.Services.AddTransient<TraceContextHandler>();
builder.Services.AddHttpClient<IArticleTextValidator, HttpArticleTextValidator>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PROFANITY_SERVICE_URL"] ?? "http://profanity-service:8080");
}).AddHttpMessageHandler<TraceContextHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseHappyHeadlinesObservability("publisher-service");
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", (IConfiguration config) => Results.Ok(new { status = "healthy", instance = config["INSTANCE_NAME"] ?? Environment.MachineName }));
app.MapPost("/publish", async (PublishArticleRequest request, IPublishedArticlePublisher publisher, IArticleTextValidator validator, CancellationToken cancellationToken) =>
{
    if (request.ArticleId == Guid.Empty || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body)) return Results.BadRequest("ArticleId, title and body are required.");
    using var activity = new ActivitySource(HappyHeadlines.Observability.ObservabilityExtensions.ActivitySourceName).StartActivity("publish article", ActivityKind.Producer);
    ArticleTextValidationResult validation;
    try { validation = await validator.ValidateAsync($"{request.Title}\n{request.Body}", cancellationToken); }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "profanity validation unavailable");
        return Results.Json(new { error = "profanity_unavailable", message = "Article was not published because profanity validation is unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    if (validation.IsProfane) return Results.BadRequest(new { error = "profanity_detected", message = "Article contains blocked language." });
    var message = new ArticlePublishedMessage(request.ArticleId, request.ArticleId, request.Title, request.Body, request.Continent, request.IsGlobal, DateTimeOffset.UtcNow);
    await publisher.PublishAsync(message, cancellationToken);
    return Results.Accepted($"/publish/{message.MessageId}", new { message.MessageId, message.ArticleId, status = "queued" });
});
app.Run();
public partial class PublisherServiceProgram { }
