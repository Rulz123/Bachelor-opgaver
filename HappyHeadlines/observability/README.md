# Central observability

OTel Collector receives OTLP logs, traces, and metrics on ports 4317/4318. It stores logs and traces in the `observability-data` volume, exports traces to Jaeger, and exposes metrics for Prometheus scraping.

- Logs: `/var/lib/otel/logs.json`
- Traces: `/var/lib/otel/traces.json`
- Collector health: `http://localhost:13133`
- Jaeger UI: `http://localhost:16686`
- Prometheus UI and query API: `http://localhost:9090`
- Prometheus scrapes Collector metrics at `otel-collector:8889` and persists TSDB data in the `prometheus-data` volume.
- Grafana local dashboard: `http://localhost:3000/d/happy-headlines-caches/happy-headlines-cache-hit-ratio` (local-only credentials are documented in the root README).
