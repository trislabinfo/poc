# User Entity

## Purpose

Represents a user account in the system. Aggregate root for user-related operations.

## Current Implementation (Phase 1)

### Properties

- `Id` (Guid): Unique identifier (inherited from `AggregateRoot<Guid>`)
- `DefaultTenantId` (Guid): Primary tenant context when a user belongs to multiple tenants
- `Email` (Email): User's email address (value object)
- `DisplayName` (string): User's display name
- `IsActive` (bool): Whether user account is active
- `CreatedAt` (DateTime): When user was created (inherited from `Entity<Guid>`)
- `UpdatedAt` (DateTime?): When user was last updated (inherited from `Entity<Guid>`)

### Methods

- `Create()`: Factory method to create new user
- `Update()`: Update display name
- `Deactivate()`: Deactivate user account

### Domain Events

- `UserCreatedEvent`: Raised when user is created
- `UserUpdatedEvent`: Raised when user is updated
- `UserDeactivatedEvent`: Raised when user is deactivated

## Planned Enhancements

### Phase 2: Security Hardening

**Properties**:

- `EmailConfirmed` (bool)
- `IsLocked` (bool)
- `LockoutEnd` (DateTime?)
- `FailedLoginAttempts` (int)

**Methods**:

- `ConfirmEmail()`
- `Lock(reason, duration)`
- `Unlock()`
- `RecordFailedLogin()`
- `ResetFailedLoginAttempts()`

### Phase 3: Advanced Features

- Password lifecycle tracking and upgrades
- MFA credentials and external providers

### Phase 4: Enterprise Scale

- Soft deletion and data anonymization
- Optimistic concurrency (row version)

## Business Rules

### Current Invariants (Phase 1)

- `DefaultTenantId` must be non-empty
- `Email` is required (uniqueness enforced by application/persistence)
- `DisplayName` cannot be empty and is limited to 100 characters

## Usage Examples

### Create User

```csharp
var emailResult = Email.Create("user@example.com");
if (emailResult.IsFailure)
    return emailResult.Error;

var userResult = User.Create(
    defaultTenantId,
    emailResult.Value,
    "John Doe",
    dateTimeProvider);

if (userResult.IsFailure)
    return userResult.Error;

await _userRepository.AddAsync(userResult.Value, cancellationToken);
```

