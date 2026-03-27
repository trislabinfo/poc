# Capabilities.Auditing

Auditing infrastructure for tracking changes and access.

## Structure

- `Interceptors/`
  - `AuditInterceptor` – EF Core interceptor capturing audit metadata.
- `Services/`
  - `IAuditService`, `AuditService` – audit logging APIs.

## Dependencies

- `BuildingBlocks.Kernel`
- `Microsoft.EntityFrameworkCore`

