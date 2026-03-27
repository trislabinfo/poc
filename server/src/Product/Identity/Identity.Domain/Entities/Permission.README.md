# Permission Entity

## Purpose

Represents a permission definition (e.g. `users.create`) that can be granted to roles.

## Current Implementation (Phase 1)

### Properties

- `Id` (Guid)
- `Code` (string): must match `module.action`
- `Name` (string)
- `Description` (string)
- `Module` (string)
- `CreatedAt` (DateTime)

### Methods

- `Create()`: Factory method with code format validation

### Domain Events

- `PermissionCreatedEvent`

## Planned Enhancements

### Phase 3

- Required permissions / permission tree
- Resource-level permissions (`ResourcePattern`)
- Dynamic permissions

