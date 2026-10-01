using HappyHeadlines.Messaging;
using HappyHeadlines.Observability;
using NewsletterService;

var builder = WebApplication.CreateBuilder(args);
builder.AddHappyHeadlinesObservability("newsletter-service");
builder.Services.AddSingleton<INewsletterRepository, PostgresNewsletterRepository>();
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddHostedService<NewsletterQueueHostedService>();
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
var app = builder.Build();
var repo = app.Services.GetRequiredService<INewsletterRepository>();
if (!app.Environment.IsEnvironment("Testing")) await repo.InitializeAsync(app.Lifetime.ApplicationStopping);
app.UseHappyHeadlinesObservability("newsletter-service"); app.UseSwagger(); app.UseSwaggerUI();
app.MapGet("/health", (IConfiguration c) => Results.Ok(new { status="healthy", instance=c["INSTANCE_NAME"] ?? Environment.MachineName }));
app.MapGet("/newsletters/{articleId:guid}", async (Guid articleId, INewsletterRepository store, CancellationToken ct) => Results.Ok(new { articleId, deliveries = await store.CountAsync(articleId, ct) }));
app.Run();
public partial class NewsletterServiceProgram { }
