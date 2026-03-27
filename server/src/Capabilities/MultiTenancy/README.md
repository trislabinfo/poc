# Capabilities.MultiTenancy

Cross-cutting multi-tenancy support used by hosts and modules.

## Structure

- `Middleware/`
  - `TenantResolutionMiddleware` – resolves the current tenant from the HTTP request.
- `Services/`
  - `ITenantService`, `TenantService` – APIs for managing and resolving tenants.
- `Context/`
  - `ITenantContext`, `TenantContext` – holds current tenant information for the request.

## Dependencies

- `BuildingBlocks.Kernel` – for shared abstractions and domain primitives.

