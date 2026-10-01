using DraftService;
using HappyHeadlines.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddHappyHeadlinesObservability("draft-service");
builder.Services.AddSingleton<IDraftRepository, PostgresDraftRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
var repository = app.Services.GetRequiredService<IDraftRepository>();
if (!app.Environment.IsEnvironment("Testing")) await repository.InitializeAsync(app.Lifetime.ApplicationStopping);

app.UseHappyHeadlinesObservability("draft-service");
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", (IConfiguration configuration) => Results.Ok(new { status = "healthy", instance = configuration["INSTANCE_NAME"] ?? Environment.MachineName }));
app.MapGet("/drafts", async (IDraftRepository store, CancellationToken cancellationToken) => Results.Ok(await store.GetAllAsync(cancellationToken)));
app.MapGet("/drafts/{id:guid}", async (Guid id, IDraftRepository store, CancellationToken cancellationToken) =>
{
    var draft = await store.GetAsync(id, cancellationToken);
    return draft is null ? Results.NotFound() : Results.Ok(draft);
});
app.MapPost("/drafts", async (CreateDraftRequest request, IDraftRepository store, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body) || string.IsNullOrWhiteSpace(request.Author)) return Results.BadRequest("Title, body and author are required.");
    var now = DateTimeOffset.UtcNow;
    var draft = new Draft(Guid.NewGuid(), request.Title, request.Body, request.Author, now, now);
    await store.CreateAsync(draft, cancellationToken);
    return Results.Created($"/drafts/{draft.Id}", draft);
});
app.MapPut("/drafts/{id:guid}", async (Guid id, UpdateDraftRequest request, IDraftRepository store, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body) || string.IsNullOrWhiteSpace(request.Author)) return Results.BadRequest("Title, body and author are required.");
    var existing = await store.GetAsync(id, cancellationToken);
    if (existing is null) return Results.NotFound();
    var draft = existing with { Title = request.Title, Body = request.Body, Author = request.Author, UpdatedAt = DateTimeOffset.UtcNow };
    await store.UpdateAsync(draft, cancellationToken);
    return Results.Ok(draft);
});
app.MapDelete("/drafts/{id:guid}", async (Guid id, IDraftRepository store, CancellationToken cancellationToken) => await store.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound());

app.Run();

public partial class DraftServiceProgram { }
