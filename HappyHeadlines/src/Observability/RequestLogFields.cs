using System.Diagnostics;

namespace HappyHeadlines.Observability;

public static class RequestLogFields
{
    public static IReadOnlyDictionary<string, object?> Create(string service, string environment, string requestId, string operation, double duration, string outcome, int statusCode, Activity? activity, Exception? error = null)
    {
        var fields = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["level"] = outcome == "success" ? "Information" : "Error",
            ["service"] = service,
            ["environment"] = environment,
            ["trace_id"] = activity?.TraceId.ToString() ?? string.Empty,
            ["span_id"] = activity?.SpanId.ToString() ?? string.Empty,
            ["request_id"] = requestId,
            ["operation"] = operation,
            ["duration"] = duration,
            ["outcome"] = outcome,
            ["status_code"] = statusCode
        };
        if (error is not null) fields["error"] = error.Message;
        return fields;
    }
}
