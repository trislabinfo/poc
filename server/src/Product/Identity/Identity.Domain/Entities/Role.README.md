# Role Entity

## Purpose

Represents a role within a tenant. Aggregate root for role state changes.

## Current Implementation (Phase 1)

### Properties

- `Id` (Guid)
- `TenantId` (Guid)
- `Name` (string)
- `Description` (string)
- `IsSystemRole` (bool)
- `IsActive` (bool)
- `CreatedAt` (DateTime)
- `UpdatedAt` (DateTime?)

### Methods

- `Create()`: Factory method
- `Update()`: Updates name/description (blocked for system roles)

### Domain Events

- `RoleCreatedEvent`
- `RoleUpdatedEvent`

## Planned Enhancements

### Phase 2

- Permissions collection (`RolePermission`) and helper methods

### Phase 3

- Role hierarchy (parent role)

### Phase 4

- Concurrency token (row version)

