# Happy Headlines Observability

Shared ASP.NET Core registration for JSON request logs, OpenTelemetry traces/metrics, request IDs, and outgoing W3C trace-context propagation.

Services call `AddHappyHeadlinesObservability` during builder setup and `UseHappyHeadlinesObservability` after app construction.
