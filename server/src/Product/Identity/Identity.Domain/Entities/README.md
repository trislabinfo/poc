## Entities

This folder contains the Identity module domain entities.

### Entities (Phase 1)

- `User` (aggregate root)
- `Credential` (entity)
- `Role` (aggregate root)
- `Permission` (aggregate root)
- `UserRole` (join entity)
- `RolePermission` (join entity)
- `RefreshToken` (entity)

### Notes

- All entities use private setters and expose state changes via methods returning `Result` / `Result<T>`.
- Parameterless constructors are private and exist only for EF Core materialization.
- Domain events are raised for key state changes.
