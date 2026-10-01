using System.Diagnostics;

namespace HappyHeadlines.Observability;

public sealed class TraceContextHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        if (activity is not null && activity.Id is not null)
        {
            request.Headers.TryAddWithoutValidation("traceparent", activity.Id);
            if (!string.IsNullOrWhiteSpace(activity.TraceStateString)) request.Headers.TryAddWithoutValidation("tracestate", activity.TraceStateString);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
