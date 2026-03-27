# Feature Module

Feature management: feature definitions, flags, and tenant-scoped feature configuration.

## Purpose

- Define and store feature and feature-flag entities per tenant.
- Support runtime feature toggles and configuration.
- Provide contracts for other modules that depend on tenant context.

## Projects

- **Feature.Module** – host integration, references Tenant.Contracts
- **Feature.Domain** – domain entities and logic
- **Feature.Application** – use cases and application services
- **Feature.Infrastructure** – persistence and external integrations
- **Feature.Contracts** – shared DTOs and API contracts
- **Feature.Migrations** – EF Core migrations for this module

## Migration dependencies

Depends on **Tenant**. Run Tenant migrations before Feature.
