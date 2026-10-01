using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HappyHeadlines.Observability;

public static class ObservabilityExtensions
{
    public const string ActivitySourceName = "HappyHeadlines";

    public static WebApplicationBuilder AddHappyHeadlinesObservability(this WebApplicationBuilder builder, string serviceName)
    {
        var environment = builder.Environment.EnvironmentName;
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://otel-collector:4317";
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "O";
            options.UseUtcTimestamp = true;
        });
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
            options.ParseStateValues = true;
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName).AddAttributes(
                [new KeyValuePair<string, object>("deployment.environment", environment)]));
            options.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
        });
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName).AddAttributes(
                [new KeyValuePair<string, object>("deployment.environment", environment)]))
            .WithTracing(tracing => tracing
                .AddSource(ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddMeter(CacheMetrics.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint)));
        return builder;
    }

    public static IApplicationBuilder UseHappyHeadlinesObservability(this IApplicationBuilder app, string serviceName)
    {
        app.Use(async (context, next) =>
        {
            var requestId = context.Request.Headers.TryGetValue("X-Request-ID", out var existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("N");
            context.Response.Headers["X-Request-ID"] = requestId;
            using var scope = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(serviceName)
                .BeginScope(new Dictionary<string, object?> { ["request_id"] = requestId });
            var stopwatch = Stopwatch.StartNew();
            var outcome = "success";
            Exception? failure = null;
            try
            {
                await next();
                if (context.Response.StatusCode >= 400) outcome = "failure";
            }
            catch (Exception exception)
            {
                outcome = "error";
                failure = exception;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                var fields = RequestLogFields.Create(
                    serviceName,
                    context.RequestServices.GetRequiredService<IHostEnvironment>().EnvironmentName,
                    requestId,
                    $"{context.Request.Method} {context.Request.Path}",
                    stopwatch.Elapsed.TotalMilliseconds,
                    outcome,
                    context.Response.StatusCode,
                    Activity.Current,
                    failure);
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(serviceName);
                using (logger.BeginScope(fields)) logger.LogInformation("request_completed");
            }
        });
        return app;
    }

    public static HttpClient AddTraceContext(this HttpClient client) => client;
}
