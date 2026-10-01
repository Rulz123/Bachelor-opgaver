using System.Diagnostics;
using HappyHeadlines.Observability;
using Xunit;

namespace ArticleService.Tests;

public class ObservabilityTests
{
    [Fact]
    public void Structured_request_fields_include_required_values_without_body()
    {
        using var activity = new Activity("test-request").SetIdFormat(ActivityIdFormat.W3C).Start();
        var fields = RequestLogFields.Create("draft-service", "Testing", "request-123", "POST /drafts", 12.5, "failure", 500, activity, new InvalidOperationException("database unavailable"));
        foreach (var key in new[] { "timestamp", "level", "service", "environment", "trace_id", "span_id", "request_id", "operation", "duration", "outcome", "status_code", "error" }) Assert.Contains(key, fields.Keys);
        Assert.DoesNotContain("body", fields.Keys);
        Assert.Equal("request-123", fields["request_id"]);
        Assert.Equal("failure", fields["outcome"]);
    }

    [Fact]
    public async Task Trace_context_handler_propagates_current_trace_id()
    {
        using var activity = new Activity("outgoing").SetIdFormat(ActivityIdFormat.W3C).Start();
        using var handler = new TraceContextHandler { InnerHandler = new CaptureHandler() };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/health");
        _ = await client.SendAsync(request);
        Assert.Equal(activity.Id, CaptureHandler.LastTraceParent);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public static string? LastTraceParent { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastTraceParent = request.Headers.GetValues("traceparent").SingleOrDefault();
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
