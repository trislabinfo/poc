# ServiceDefaults

Shared Aspire service defaults for logging, health checks, telemetry, and HTTP resilience.

## Purpose

- **OpenTelemetry**: metrics (ASP.NET Core, HTTP client, runtime) and tracing; OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.
- **Health checks**: default liveness check; in Development, `/health` and `/alive` are mapped.
- **Service discovery**: for distributed apps under Aspire.
- **HTTP resilience**: standard resilience handler and service discovery on `HttpClient`.

## Usage

1. Add a project reference to `ServiceDefaults` from your host or service.
2. In `Program.cs`, after `WebApplication.CreateBuilder(args)` call:
   - `builder.AddServiceDefaults();`
3. After `builder.Build()`, call:
   - `app.MapDefaultEndpoints();`

Only add ServiceDefaults to projects that participate in Aspire orchestration or need the same observability and health contract.
