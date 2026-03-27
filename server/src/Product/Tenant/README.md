# Tenant Module

Tenant management: organizations (tenants), subscriptions, and tenant-scoped configuration.

## Purpose

- Define and store tenant/organization entities.
- Support subscription and tenant lifecycle.
- Provide tenant context for multi-tenant hosts.

## Projects

- **Tenant.Module** – host integration and registration
- **Tenant.Domain** – domain entities and logic
- **Tenant.Application** – use cases and application services
- **Tenant.Infrastructure** – persistence and external integrations
- **Tenant.Contracts** – shared DTOs and API contracts
- **Tenant.Migrations** – EF Core migrations for this module

## Migration dependencies

None (root module). Other modules (Identity, User, Feature) depend on Tenant.
