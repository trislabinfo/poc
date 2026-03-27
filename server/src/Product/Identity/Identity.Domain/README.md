## Identity.Domain

Domain model for the Identity module. This project is intentionally free of infrastructure concerns (no EF Core, no ASP.NET Core, no MediatR handlers).

### Current Architecture (Phase 1)

- **Entities (7)**:
  - `User`
  - `Credential`
  - `Role`
  - `Permission`
  - `UserRole`
  - `RolePermission`
  - `RefreshToken`
- **Value Objects (2)**:
  - `Email`
  - `PasswordHash`
- **Domain Events (10)**:
  - `UserCreatedEvent`
  - `UserUpdatedEvent`
  - `UserDeactivatedEvent`
  - `RoleCreatedEvent`
  - `RoleUpdatedEvent`
  - `PermissionCreatedEvent`
  - `UserRoleAssignedEvent`
  - `UserRoleRemovedEvent`
  - `RefreshTokenCreatedEvent`
  - `RefreshTokenRevokedEvent`
- **Repository Interfaces (4)**:
  - `IUserRepository`
  - `IRoleRepository`
  - `IPermissionRepository`
  - `IRefreshTokenRepository`
- **Domain Services (1)**:
  - `IPasswordHasher`
- **Enums (1)**:
  - `CredentialType`

### Planned Architecture

- **Future Value Objects**:
  - `PasswordPolicy`, `ConsentVersion`, `IpAddress`, `EncryptedValue<T>`, `DataRetentionPolicy`
- **Future Domain Services**:
  - `IPasswordPolicyValidator`, `IPermissionChecker`, `IAccountLockoutPolicy`, `IDataAnonymizer`
- **Future Specifications**:
  - `ActiveUsersSpecification`, `UsersWithExpiredPasswordsSpecification`, `LockedAccountsSpecification`
- **Future Entities**:
  - `PasswordHistory`, `LoginAttempt`, `UserConsent`, `Session`

### Migration Guide

- **Adding new properties to entities**:
  - Add private setters
  - Set defaults in factory methods
  - Update state via methods returning `Result`
  - Add related domain events where appropriate
- **Adding new value objects**:
  - Use `ValueObject` base type
  - Implement `GetEqualityComponents()`
  - Prefer `Create()` factories returning `Result<T>`
- **Adding new domain services**:
  - Define interface in Domain
  - Implement in Infrastructure
  - Inject and use from Application layer

### Design Decisions

- **Why Result pattern**: explicit, testable error handling without exception-based flow control.
- **Why domain events**: capture important state transitions to enable eventual integration/outbox patterns.
- **Why guard/validation**: enforce invariants at boundaries and within state transitions.
- **Why IDateTimeProvider**: deterministic testing and consistent UTC time source.
