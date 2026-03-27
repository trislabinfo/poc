# User Module

User management: profiles, settings, and user-scoped data within a tenant.

## Purpose

- Manage user profiles and settings after identity is established.
- Support user-specific configuration and preferences.
- Depends on Tenant and Identity for tenant and identity context.

## Projects

- **User.Module** – host integration, references Tenant.Contracts and Identity.Contracts
- **User.Domain** – domain entities and logic
- **User.Application** – use cases and application services
- **User.Infrastructure** – persistence and external integrations
- **User.Contracts** – shared DTOs and API contracts
- **User.Migrations** – EF Core migrations for this module

## Migration dependencies

Depends on **Tenant** and **Identity**. Run their migrations before User.
