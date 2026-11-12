using System.Text.Json;
using System.Text.Json.Serialization;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using SubscriberService.Data;

var builder = WebApplication.CreateBuilder(args);
var serviceName = builder.Environment.ApplicationName;
var seqUrl = builder.Configuration["SeqUrl"]
    ?? "http://localhost:5341";
// Add services to the container.
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService(serviceName));

    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;

    logging.AddOtlpExporter(exporter =>
    {
        exporter.Endpoint =
            new Uri($"{seqUrl}/ingest/otlp/v1/logs");

        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddSource("HappyHeadlines.Messaging");

        tracing.AddOtlpExporter(exporter =>
        {
            exporter.Endpoint =
                new Uri($"{seqUrl}/ingest/otlp/v1/traces");

            exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
        });
    });

builder.Services.AddHttpClient("SubscriberService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["SubscriberServiceUrl"]
        ?? "http://localhost:5001");

    client.Timeout = TimeSpan.FromSeconds(3);
});

var rabbitMqFactory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "student",
    Password = "schoolproject"
};

await using var rabbitMqConnection =
    await rabbitMqFactory.CreateConnectionAsync();
builder.Services.AddSingleton<IConnection>(rabbitMqConnection);
builder.Services.AddSingleton<JsonSerializerOptions>(
    new JsonSerializerOptions(JsonSerializerDefaults.Web)
);
builder.Services.AddDbContext<SubscriberDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSwaggerGen();
await using (var channel =
    await rabbitMqConnection.CreateChannelAsync())
{
    await channel.ExchangeDeclareAsync(
        exchange: "subscriber.published",
        type: ExchangeType.Fanout,
        durable: true,
        autoDelete: false);

    foreach (var queueName in new[]
    {
        "subscriber.newsletter"
    })
    {
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: "subscriber.published",
            routingKey: string.Empty);
    }
}
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
