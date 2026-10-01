using Xunit;

namespace ArticleService.Tests;

public class CentralObservabilityTests
{
    [Fact]
    public void Collector_configuration_has_persistent_log_and_trace_exporters()
    {
        string? path = null;
        var repositoryRoot = Environment.GetEnvironmentVariable("HAPPY_HEADLINES_ROOT");
        if (!string.IsNullOrWhiteSpace(repositoryRoot))
        {
            var candidate = Path.Combine(repositoryRoot, "observability", "otel-collector-config.yaml");
            if (File.Exists(candidate)) path = candidate;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "observability", "otel-collector-config.yaml");
            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }
            directory = directory.Parent;
        }
        Assert.NotNull(path);
        var configuration = File.ReadAllText(path!);
        Assert.Contains("file/logs:", configuration);
        Assert.Contains("file/traces:", configuration);
        Assert.Contains("path: /var/lib/otel/logs.json", configuration);
        Assert.Contains("path: /var/lib/otel/traces.json", configuration);
        Assert.Contains("otlp/jaeger:", configuration);
        Assert.Contains("prometheus:", configuration);
        Assert.Contains("exporters: [prometheus]", configuration);
        var prometheusConfigurationPath = Path.Combine(Path.GetDirectoryName(path!)!, "prometheus.yml");
        var prometheusConfiguration = File.ReadAllText(prometheusConfigurationPath);
        Assert.Contains("job_name: otel-collector", prometheusConfiguration);
        Assert.Contains("otel-collector:8889", prometheusConfiguration);

        var dashboardPath = Path.Combine(Path.GetDirectoryName(path!)!, "grafana", "dashboards", "cache-hit-ratio.json");
        using var dashboard = System.Text.Json.JsonDocument.Parse(File.ReadAllText(dashboardPath));
        var dashboardQueries = dashboard.RootElement.GetProperty("panels")
            .EnumerateArray()
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray())
            .Select(target => target.GetProperty("expr").GetString())
            .ToArray();
        Assert.Contains(dashboardQueries, query => query!.Contains("cache_name=\"article_cache\"", StringComparison.Ordinal));
        Assert.Contains(dashboardQueries, query => query!.Contains("cache_name=\"comment_cache\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Grafana_dashboard_queries_article_and_comment_cache_hit_ratios()
    {
        var repositoryRoot = Environment.GetEnvironmentVariable("HAPPY_HEADLINES_ROOT");
        if (string.IsNullOrWhiteSpace(repositoryRoot))
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "compose.yaml"))) directory = directory.Parent;
            repositoryRoot = directory?.FullName;
        }

        Assert.NotNull(repositoryRoot);
        var dashboardPath = Path.Combine(repositoryRoot!, "observability", "grafana", "dashboards", "cache-hit-ratio.json");
        using var dashboard = System.Text.Json.JsonDocument.Parse(File.ReadAllText(dashboardPath));
        var queries = dashboard.RootElement.GetProperty("panels")
            .EnumerateArray()
            .SelectMany(panel => panel.GetProperty("targets").EnumerateArray())
            .Select(target => target.GetProperty("expr").GetString())
            .ToArray();
        Assert.Contains(queries, query => query!.Contains("cache_name=\"article_cache\"", StringComparison.Ordinal));
        Assert.Contains(queries, query => query!.Contains("cache_name=\"comment_cache\"", StringComparison.Ordinal));
    }
}
