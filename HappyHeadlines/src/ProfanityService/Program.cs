using ProfanityService;
using HappyHeadlines.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddHappyHeadlinesObservability("profanity-service");
builder.Services.AddSingleton<IProfanityRepository, PostgresProfanityRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseHappyHeadlinesObservability("profanity-service");
var repository = app.Services.GetRequiredService<IProfanityRepository>();
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
app.MapPost("/validate", async (ValidateTextRequest request, IProfanityRepository store, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text)) return Results.BadRequest("Text is required.");
    return Results.Ok(await store.ValidateAsync(request.Text, cancellationToken));
});

app.Run();

public partial class Program { }
