# Identity Module Implementation Plan

## Overview

This plan implements the **Identity module** with a **minimal viable domain layer** that supports basic CRUD operations while establishing enterprise-ready patterns and documenting future enhancements.

**Philosophy**: Start simple, build enterprise-ready foundation, document future complexity.

---

## Phase 1: Domain Layer - Minimal Viable Implementation (9 hours)

**Status**: ✅ Completed

**Objective**: Create domain entities with core properties, basic CRUD operations, and enterprise patterns (Result, domain events, Guard clauses).

### 1.1: BuildingBlocks.Kernel Enhancements (1.5 hours)

#### Tasks:

1. **Verify/Create Base Classes** (30 minutes)
   - [x] Verify `Entity<TId>` exists in `BuildingBlocks.Kernel/Domain/Entity.cs`
     - Should have: `Id` property, `CreatedAt`, `UpdatedAt`, domain events collection
   - [x] Verify `AggregateRoot<TId>` exists in `BuildingBlocks.Kernel/Domain/AggregateRoot.cs`
     - Should inherit from `Entity<TId>`
   - [x] Verify `ValueObject` exists in `BuildingBlocks.Kernel/Domain/ValueObject.cs`
     - Should have: equality comparison methods
   - [x] Verify `DomainEvent` exists in `BuildingBlocks.Kernel/Domain/DomainEvent.cs`
     - Should have: `OccurredOn` timestamp property
   - [x] Verify `DomainException` exists in `BuildingBlocks.Kernel/Exceptions/DomainException.cs`
     - Should inherit from `Exception`
   - [ ] If any are missing, create them following existing patterns

2. **Create Guard Class** (15 minutes)
   - [x] Create `Guard.cs` in `BuildingBlocks.Kernel/Domain/`
   - [x] Add methods:
     - `Against.Null<T>(T value, string parameterName)`
     - `Against.NullOrEmpty(string value, string parameterName)`
     - `Against.InvalidEmail(string email, string parameterName)`
     - `Against.OutOfRange<T>(T value, T min, T max, string parameterName)`
   - [x] Each method throws `ArgumentException` with descriptive message
   - [x] Add XML documentation

3. **Create IDateTimeProvider Interface** (10 minutes)
   - [x] Create `IDateTimeProvider.cs` in `BuildingBlocks.Kernel/Domain/`
   - [x] Add `DateTime UtcNow { get; }` property
   - [x] Add XML documentation explaining testability purpose
   - [x] Create `SystemDateTimeProvider.cs` implementation returning `DateTime.UtcNow`

4. **Create DomainInvariantException** (10 minutes)
   - [x] Create `DomainInvariantException.cs` in `BuildingBlocks.Kernel/Exceptions/`
   - [x] Inherit from `DomainException`
   - [x] Add constructor accepting message and inner exception
   - [x] Add XML documentation

5. **Verify Result<T> Pattern** (15 minutes)
   - [x] Verify `Result` and `Result<T>` exist in `BuildingBlocks.Kernel/Results/`
   - [x] Should have: `IsSuccess`, `IsFailure`, `Value`, `Error` properties
   - [x] Should have: `Success()`, `Failure()` factory methods
   - [ ] If missing, create following Railway Oriented Programming pattern

**Deliverable**: BuildingBlocks.Kernel has all base classes, Guard, IDateTimeProvider, DomainInvariantException, Result<T>

---

### 1.2: Value Objects (40 minutes)

**Status**: ✅ Completed

#### Email Value Object

**File**: `Identity.Domain/ValueObjects/Email.cs`

**Base Class**: Inherit from `ValueObject`

**Properties**:
- `string Value { get; }` (init-only or private set)

**Methods**:
- `static Result<Email> Create(string value)` - Factory with validation
- `protected override IEnumerable<object> GetEqualityComponents()` - For ValueObject equality

**Validation** (in Create method):
- `Guard.Against.NullOrEmpty(value, nameof(value))`
- Valid email format (regex: `^[^@\s]+@[^@\s]+\.[^@\s]+$`)
- Max length 254 characters
- Return `Result<Email>.Failure("Invalid email")` on validation failure

**Constructor**:
- Private constructor accepting validated string value

**Future Enhancements** (document in code comments):
```csharp
// TODO Phase 2: Email normalization (lowercase, trim)
// TODO Phase 3: Disposable email detection
// TODO Phase 3: Domain blacklist/whitelist
```

---

#### PasswordHash Value Object

**File**: `Identity.Domain/ValueObjects/PasswordHash.cs`

**Base Class**: Inherit from `ValueObject`

**Properties**:
- `string Hash { get; }` (init-only or private set)

**Methods**:
- `static Result<PasswordHash> Create(string hash)` - Factory with validation
- `protected override IEnumerable<object> GetEqualityComponents()` - For ValueObject equality

**Validation** (in Create method):
- `Guard.Against.NullOrEmpty(hash, nameof(hash))`
- Return `Result<PasswordHash>.Failure("Invalid password hash")` on validation failure

**Constructor**:
- Private constructor accepting validated hash string

**Future Enhancements** (document in code comments):
```csharp
// TODO Phase 3: Algorithm tracking (bcrypt, argon2)
// TODO Phase 3: Work factor tracking
// TODO Phase 3: NeedsRehash() method for algorithm upgrades
```

---

### 1.3: Domain Events (50 minutes)

**Status**: ✅ Completed

**Location**: `Identity.Domain/Events/`

**Base Class**: All events inherit from `DomainEvent` (record types)

**Events to Create**:

1. **UserCreatedEvent.cs**
```csharp
public record UserCreatedEvent(
    Guid UserId,
    Guid DefaultTenantId, // primary tenant context when user belongs to multiple tenants
    string Email,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

2. **UserUpdatedEvent.cs**
```csharp
public record UserUpdatedEvent(
    Guid UserId,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

3. **UserDeactivatedEvent.cs**
```csharp
public record UserDeactivatedEvent(
    Guid UserId,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

4. **RoleCreatedEvent.cs**
```csharp
public record RoleCreatedEvent(
    Guid RoleId,
    Guid TenantId,
    string Name,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

5. **RoleUpdatedEvent.cs**
```csharp
public record RoleUpdatedEvent(
    Guid RoleId,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

6. **PermissionCreatedEvent.cs**
```csharp
public record PermissionCreatedEvent(
    Guid PermissionId,
    string Code,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

7. **UserRoleAssignedEvent.cs**
```csharp
public record UserRoleAssignedEvent(
    Guid UserId,
    Guid RoleId,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

8. **UserRoleRemovedEvent.cs**
```csharp
public record UserRoleRemovedEvent(
    Guid UserId,
    Guid RoleId,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

9. **RefreshTokenCreatedEvent.cs**
```csharp
public record RefreshTokenCreatedEvent(
    Guid TokenId,
    Guid UserId,
    DateTime ExpiresAt,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

10. **RefreshTokenRevokedEvent.cs**
```csharp
public record RefreshTokenRevokedEvent(
    Guid TokenId,
    Guid UserId,
    DateTime OccurredOn
) : DomainEvent(OccurredOn);
```

**Future Events** (document in code comments at bottom of each file):
```csharp
// TODO Phase 2: EmailConfirmedEvent
// TODO Phase 2: UserLockedEvent, UserUnlockedEvent
// TODO Phase 2: PasswordChangedEvent, PasswordResetEvent
// TODO Phase 2: FailedLoginRecordedEvent, SuccessfulLoginRecordedEvent
// TODO Phase 3: ConsentGrantedEvent, ConsentRevokedEvent
// TODO Phase 4: UserMarkedForDeletionEvent, UserAnonymizedEvent
```

---

### 1.4: Entities (3.5 hours)

**Status**: ✅ Completed

#### User Entity (Aggregate Root)

**File**: `Identity.Domain/Entities/User.cs`

**Base Class**: `AggregateRoot<Guid>`

**Current Properties**:
```csharp
public Guid DefaultTenantId { get; private set; }
public Email Email { get; private set; }
public string DisplayName { get; private set; }
public bool IsActive { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<User> Create(
    Guid defaultTenantId,
    Email email,
    string displayName,
    IDateTimeProvider dateTimeProvider)
{
    Guard.Against.Null(email, nameof(email));
    Guard.Against.NullOrEmpty(displayName, nameof(displayName));
    
    var user = new User
    {
        Id = Guid.NewGuid(),
        DefaultTenantId = defaultTenantId,
        Email = email,
        DisplayName = displayName,
        IsActive = true,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    user.RaiseDomainEvent(new UserCreatedEvent(
        user.Id,
        user.DefaultTenantId,
        user.Email.Value,
        dateTimeProvider.UtcNow
    ));
    
    return Result<User>.Success(user);
}
```

2. **Update Method**:
```csharp
public Result Update(string displayName, IDateTimeProvider dateTimeProvider)
{
    Guard.Against.NullOrEmpty(displayName, nameof(displayName));
    
    DisplayName = displayName;
    UpdatedAt = dateTimeProvider.UtcNow;
    
    RaiseDomainEvent(new UserUpdatedEvent(Id, dateTimeProvider.UtcNow));
    
    return Result.Success();
}
```

3. **Deactivate Method**:
```csharp
public Result Deactivate(IDateTimeProvider dateTimeProvider)
{
    if (!IsActive)
        return Result.Failure("User is already deactivated");
    
    IsActive = false;
    UpdatedAt = dateTimeProvider.UtcNow;
    
    RaiseDomainEvent(new UserDeactivatedEvent(Id, dateTimeProvider.UtcNow));
    
    return Result.Success();
}
```

**Private Constructor**:
```csharp
private User() { } // For EF Core
```

**Future Properties** (document in `#region Future Properties - Phase 2`):
```csharp
#region Future Properties - Phase 2
// TODO Phase 2: public bool EmailConfirmed { get; private set; }
// TODO Phase 2: public bool IsLocked { get; private set; }
// TODO Phase 2: public DateTime? LockoutEnd { get; private set; }
// TODO Phase 2: public int FailedLoginAttempts { get; private set; }
// TODO Phase 3: public DateTime? LastPasswordChangedAt { get; private set; }
// TODO Phase 3: public DateTime? PasswordExpiresAt { get; private set; }
// TODO Phase 4: public DateTime? DeletedAt { get; private set; }
// TODO Phase 4: public byte[] RowVersion { get; private set; }
#endregion
```

**Future Methods** (document in `#region Future Methods - Phase 2`):
```csharp
#region Future Methods - Phase 2
// TODO Phase 2: public Result ConfirmEmail()
// TODO Phase 2: public Result Lock(string reason, TimeSpan? duration)
// TODO Phase 2: public Result Unlock()
// TODO Phase 2: public Result RecordFailedLogin()
// TODO Phase 2: public Result ResetFailedLoginAttempts()
// TODO Phase 3: public Result ChangePassword()
// TODO Phase 4: public Result MarkForDeletion()
// TODO Phase 4: public Result AnonymizePersonalData()
#endregion
```

---

#### Credential Entity

**File**: `Identity.Domain/Entities/Credential.cs`

**Base Class**: `Entity<Guid>`

**Current Properties**:
```csharp
public Guid UserId { get; private set; }
public CredentialType Type { get; private set; }
public PasswordHash PasswordHash { get; private set; }
public bool IsActive { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<Credential> CreatePassword(
    Guid userId,
    PasswordHash passwordHash,
    IDateTimeProvider dateTimeProvider)
{
    Guard.Against.Null(passwordHash, nameof(passwordHash));
    
    var credential = new Credential
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = CredentialType.Password,
        PasswordHash = passwordHash,
        IsActive = true,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    return Result<Credential>.Success(credential);
}
```

**Private Constructor**:
```csharp
private Credential() { } // For EF Core
```

**Future Properties** (document in comments):
```csharp
#region Future Properties - Phase 3
// TODO Phase 3: public string? ExternalProviderId { get; private set; }
// TODO Phase 3: public string? ExternalProvider { get; private set; }
// TODO Phase 3: public EncryptedValue<string>? MfaSecret { get; private set; }
// TODO Phase 3: public EncryptedValue<string[]>? MfaBackupCodes { get; private set; }
// TODO Phase 3: public DateTime? LastUsedAt { get; private set; }
#endregion
```

**Future Methods**:
```csharp
#region Future Methods - Phase 3
// TODO Phase 3: public static Result<Credential> CreateExternalProvider(...)
// TODO Phase 3: public static Result<Credential> CreateMfa(...)
// TODO Phase 3: public Result RecordUsage()
#endregion
```

---

#### Role Entity (Aggregate Root)

**File**: `Identity.Domain/Entities/Role.cs`

**Base Class**: `AggregateRoot<Guid>`

**Current Properties**:
```csharp
public Guid TenantId { get; private set; }
public string Name { get; private set; }
public string Description { get; private set; }
public bool IsSystemRole { get; private set; }
public bool IsActive { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<Role> Create(
    Guid tenantId,
    string name,
    string description,
    bool isSystemRole,
    IDateTimeProvider dateTimeProvider)
{
    Guard.Against.NullOrEmpty(name, nameof(name));
    Guard.Against.NullOrEmpty(description, nameof(description));
    
    var role = new Role
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Name = name,
        Description = description,
        IsSystemRole = isSystemRole,
        IsActive = true,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    role.RaiseDomainEvent(new RoleCreatedEvent(
        role.Id,
        role.TenantId,
        role.Name,
        dateTimeProvider.UtcNow
    ));
    
    return Result<Role>.Success(role);
}
```

2. **Update Method**:
```csharp
public Result Update(string name, string description, IDateTimeProvider dateTimeProvider)
{
    Guard.Against.NullOrEmpty(name, nameof(name));
    Guard.Against.NullOrEmpty(description, nameof(description));
    
    if (IsSystemRole)
        return Result.Failure("Cannot update system role");
    
    Name = name;
    Description = description;
    UpdatedAt = dateTimeProvider.UtcNow;
    
    RaiseDomainEvent(new RoleUpdatedEvent(Id, dateTimeProvider.UtcNow));
    
    return Result.Success();
}
```

**Private Constructor**:
```csharp
private Role() { } // For EF Core
```

**Future Properties**:
```csharp
#region Future Properties - Phase 2
// TODO Phase 2: public ICollection<RolePermission> Permissions { get; private set; }
// TODO Phase 3: public Guid? ParentRoleId { get; private set; }
// TODO Phase 4: public byte[] RowVersion { get; private set; }
#endregion
```

**Future Methods**:
```csharp
#region Future Methods - Phase 2
// TODO Phase 2: public Result AddPermission(Guid permissionId)
// TODO Phase 2: public Result RemovePermission(Guid permissionId)
// TODO Phase 2: public bool HasPermission(string permissionCode)
#endregion
```

---

#### Permission Entity (Aggregate Root)

**File**: `Identity.Domain/Entities/Permission.cs`

**Base Class**: `AggregateRoot<Guid>`

**Current Properties**:
```csharp
public string Code { get; private set; } // e.g., "users.create"
public string Name { get; private set; }
public string Description { get; private set; }
public string Module { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<Permission> Create(
    string code,
    string name,
    string description,
    string module,
    IDateTimeProvider dateTimeProvider)
{
    Guard.Against.NullOrEmpty(code, nameof(code));
    Guard.Against.NullOrEmpty(name, nameof(name));
    Guard.Against.NullOrEmpty(description, nameof(description));
    Guard.Against.NullOrEmpty(module, nameof(module));
    
    // Validate code format: "module.action"
    if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[a-z]+\.[a-z]+$"))
        return Result<Permission>.Failure("Permission code must be in format 'module.action'");
    
    var permission = new Permission
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Description = description,
        Module = module,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    permission.RaiseDomainEvent(new PermissionCreatedEvent(
        permission.Id,
        permission.Code,
        dateTimeProvider.UtcNow
    ));
    
    return Result<Permission>.Success(permission);
}
```

**Private Constructor**:
```csharp
private Permission() { } // For EF Core
```

**Future Properties**:
```csharp
#region Future Properties - Phase 3
// TODO Phase 3: public ICollection<Guid> RequiredPermissions { get; private set; }
// TODO Phase 3: public string? ResourcePattern { get; private set; } // for resource-level permissions
// TODO Phase 3: public bool IsDynamic { get; private set; }
#endregion
```

**Future Methods**:
```csharp
#region Future Methods - Phase 3
// TODO Phase 3: public Result AddRequiredPermission(Guid permissionId)
// TODO Phase 3: public bool MatchesResource(string resourceId)
#endregion
```

---

#### UserRole Entity (Join Table)

**File**: `Identity.Domain/Entities/UserRole.cs`

**Base Class**: `Entity<Guid>` (or composite key if preferred)

**Current Properties**:
```csharp
public Guid UserId { get; private set; }
public Guid RoleId { get; private set; }
public DateTime AssignedAt { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<UserRole> Create(
    Guid userId,
    Guid roleId,
    IDateTimeProvider dateTimeProvider)
{
    var userRole = new UserRole
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        RoleId = roleId,
        AssignedAt = dateTimeProvider.UtcNow,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    userRole.RaiseDomainEvent(new UserRoleAssignedEvent(
        userId,
        roleId,
        dateTimeProvider.UtcNow
    ));
    
    return Result<UserRole>.Success(userRole);
}
```

**Private Constructor**:
```csharp
private UserRole() { } // For EF Core
```

**Future Properties**:
```csharp
#region Future Properties - Phase 3
// TODO Phase 3: public DateTime? ExpiresAt { get; private set; }
// TODO Phase 3: public Guid? AssignedBy { get; private set; }
// TODO Phase 3: public bool IsConditional { get; private set; }
#endregion
```

**Future Methods**:
```csharp
#region Future Methods - Phase 3
// TODO Phase 3: public bool IsExpired()
// TODO Phase 3: public Result Extend(TimeSpan duration)
#endregion
```

---

#### RolePermission Entity (Join Table)

**File**: `Identity.Domain/Entities/RolePermission.cs`

**Base Class**: `Entity<Guid>` (or composite key if preferred)

**Current Properties**:
```csharp
public Guid RoleId { get; private set; }
public Guid PermissionId { get; private set; }
public DateTime GrantedAt { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<RolePermission> Create(
    Guid roleId,
    Guid permissionId,
    IDateTimeProvider dateTimeProvider)
{
    var rolePermission = new RolePermission
    {
        Id = Guid.NewGuid(),
        RoleId = roleId,
        PermissionId = permissionId,
        GrantedAt = dateTimeProvider.UtcNow,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    return Result<RolePermission>.Success(rolePermission);
}
```

**Private Constructor**:
```csharp
private RolePermission() { } // For EF Core
```

**Future Properties**:
```csharp
#region Future Properties - Phase 3
// TODO Phase 3: public DateTime? ExpiresAt { get; private set; }
// TODO Phase 3: public string? Constraints { get; private set; } // JSON constraints
// TODO Phase 3: public Guid? GrantedBy { get; private set; }
#endregion
```

**Future Methods**:
```csharp
#region Future Methods - Phase 3
// TODO Phase 3: public bool IsExpired()
// TODO Phase 3: public bool MeetsConstraints(object context)
#endregion
```

---

#### RefreshToken Entity

**File**: `Identity.Domain/Entities/RefreshToken.cs`

**Base Class**: `Entity<Guid>`

**Current Properties**:
```csharp
public Guid UserId { get; private set; }
public string Token { get; private set; } // hashed
public DateTime ExpiresAt { get; private set; }
public bool IsRevoked { get; private set; }
public DateTime? RevokedAt { get; private set; }
```

**Current Methods**:

1. **Factory Method**:
```csharp
public static Result<RefreshToken> Create(
    Guid userId,
    string token,
    TimeSpan expiresIn,
    IDateTimeProvider dateTimeProvider)
{
    Guard.Against.NullOrEmpty(token, nameof(token));
    
    var refreshToken = new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Token = token, // Should be hashed by caller
        ExpiresAt = dateTimeProvider.UtcNow.Add(expiresIn),
        IsRevoked = false,
        CreatedAt = dateTimeProvider.UtcNow
    };
    
    refreshToken.RaiseDomainEvent(new RefreshTokenCreatedEvent(
        refreshToken.Id,
        userId,
        refreshToken.ExpiresAt,
        dateTimeProvider.UtcNow
    ));
    
    return Result<RefreshToken>.Success(refreshToken);
}
```

2. **IsExpired Method**:
```csharp
public bool IsExpired(IDateTimeProvider dateTimeProvider)
{
    return dateTimeProvider.UtcNow > ExpiresAt;
}
```

3. **Revoke Method**:
```csharp
public Result Revoke(IDateTimeProvider dateTimeProvider)
{
    if (IsRevoked)
        return Result.Failure("Token is already revoked");
    
    IsRevoked = true;
    RevokedAt = dateTimeProvider.UtcNow;
    UpdatedAt = dateTimeProvider.UtcNow;
    
    RaiseDomainEvent(new RefreshTokenRevokedEvent(
        Id,
        UserId,
        dateTimeProvider.UtcNow
    ));
    
    return Result.Success();
}
```

**Private Constructor**:
```csharp
private RefreshToken() { } // For EF Core
```

**Future Properties**:
```csharp
#region Future Properties - Phase 3
// TODO Phase 3: public Guid? ReplacedByToken { get; private set; }
// TODO Phase 3: public string? DeviceInfo { get; private set; }
// TODO Phase 3: public IpAddress? IpAddress { get; private set; }
// TODO Phase 3: public string? RevocationReason { get; private set; }
#endregion
```

**Future Methods**:
```csharp
#region Future Methods - Phase 3
// TODO Phase 3: public Result ReplaceWith(RefreshToken newToken)
// TODO Phase 3: public bool IsPartOfStolenFamily()
#endregion
```

---

### 1.5: Enums (5 minutes)

**Status**: ✅ Completed

**File**: `Identity.Domain/Enums/CredentialType.cs`

```csharp
namespace Identity.Domain.Enums;

/// <summary>
/// Types of credentials a user can have
/// </summary>
public enum CredentialType
{
    /// <summary>
    /// Password-based authentication
    /// </summary>
    Password = 1,
    
    /// <summary>
    /// External provider authentication (Google, Microsoft, etc.)
    /// </summary>
    /// <remarks>TODO Phase 3: Implement external provider authentication</remarks>
    ExternalProvider = 2,
    
    /// <summary>
    /// Multi-factor authentication
    /// </summary>
    /// <remarks>TODO Phase 3: Implement MFA</remarks>
    Mfa = 3
}
```

**Future Enums** (document in comments):
```csharp
// TODO Phase 2: ConsentType (TermsOfService, DataProcessing, Marketing)
// TODO Phase 2: FailureReason (InvalidPassword, AccountLocked, EmailNotConfirmed)
// TODO Phase 2: LockoutReason (TooManyFailedAttempts, AdminAction, SecurityBreach)
```

---

### 1.6: Domain Exceptions (REMOVED - Using Result<T> Pattern)

**Decision**: All error scenarios will be handled via `Result<T>` pattern with appropriate `Error` types instead of exceptions.

**Rationale**:
- ✅ Better performance (no exception overhead)
- ✅ Explicit error handling in method signatures
- ✅ Consistent error handling across all layers
- ✅ Easier to test and reason about
- ✅ No hidden control flow via exceptions

**Error Handling Strategy**:

All domain methods return `Result` or `Result<T>`:

```csharp
// Entity not found
return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{userId}' was not found"));

// Duplicate email
return Result.Failure(Error.Conflict("User.DuplicateEmail", $"Email '{email}' already exists"));

// Invalid credentials
return Result.Failure(Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password"));

// Validation failures
return Result.Failure(Error.Validation("User.InvalidEmail", "Email format is invalid"));

// Business rule violations
return Result.Failure(Error.Failure("User.AlreadyDeactivated", "User is already deactivated"));
```

**Error Types** (from `BuildingBlocks.Kernel`):
- `Error.NotFound()` - Entity not found (404)
- `Error.Conflict()` - Duplicate/conflict (409)
- `Error.Unauthorized()` - Authentication failures (401)
- `Error.Forbidden()` - Authorization failures (403)
- `Error.Validation()` - Validation failures (400)
- `Error.Failure()` - General business rule violations (400)

**Example Domain Method**:
```csharp
public static Result<User> Create(
    Guid defaultTenantId,
    Email email,
    string displayName)
{
    if (string.IsNullOrWhiteSpace(displayName))
        return Result<User>.Failure(
            Error.Validation("User.InvalidDisplayName", "Display name cannot be empty"));
    
    if (displayName.Length > 100)
        return Result<User>.Failure(
            Error.Validation("User.DisplayNameTooLong", "Display name cannot exceed 100 characters"));
    
    var user = new User
    {
        Id = Guid.NewGuid(),
        DefaultTenantId = defaultTenantId,
        Email = email,
        DisplayName = displayName,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
    
    user.RaiseDomainEvent(new UserCreatedEvent(
        user.Id,
        user.DefaultTenantId,
        user.Email.Value,
        DateTime.UtcNow));
    
    return Result<User>.Success(user);
}
```

**Repository Pattern**:
```csharp
// Repositories return null for not found, application layer handles it
var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
if (user is null)
    return Result.Failure(Error.NotFound("User.NotFound", $"User with ID '{userId}' was not found"));
```

**Application Layer Example**:
```csharp
public async Task<Result<Guid>> Handle(
    CreateUserCommand request,
    CancellationToken cancellationToken)
{
    // Check for duplicate email
    var existingUser = await _userRepository.GetByEmailAsync(
        request.Email,
        cancellationToken);
    
    if (existingUser is not null)
        return Result<Guid>.Failure(
            Error.Conflict("User.DuplicateEmail", $"Email '{request.Email}' already exists"));
    
    // Create value objects
    var emailResult = Email.Create(request.Email);
    if (emailResult.IsFailure)
        return Result<Guid>.Failure(emailResult.Error);
    
    var passwordHashResult = PasswordHash.Create(request.PasswordHash);
    if (passwordHashResult.IsFailure)
        return Result<Guid>.Failure(passwordHashResult.Error);
    
    // Create entity
    var userResult = User.Create(
        request.DefaultTenantId,
        emailResult.Value,
        request.DisplayName);
    
    if (userResult.IsFailure)
        return Result<Guid>.Failure(userResult.Error);
    
    // Persist
    await _userRepository.AddAsync(userResult.Value, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    return Result<Guid>.Success(userResult.Value.Id);
}
```

**Time Saved**: 25 minutes (exception creation removed)

---

### 1.7: Repository Interfaces (1 hour)

**Status**: ✅ Completed

**Location**: `Identity.Domain/Repositories/`

#### IUserRepository.cs

```csharp
namespace Identity.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
    
    #region Future Methods - Phase 2
    // TODO Phase 2: Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
    // TODO Phase 2: Task<IEnumerable<User>> GetActiveUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    // TODO Phase 2: Task<IEnumerable<User>> GetLockedUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<User>> GetUsersWithExpiredPasswordsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    #endregion
}
```

---

#### IRoleRepository.cs

```csharp
namespace Identity.Domain.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
    
    #region Future Methods - Phase 2
    // TODO Phase 2: Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    // TODO Phase 2: Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<Role>> GetRoleHierarchyAsync(Guid roleId, CancellationToken cancellationToken = default);
    #endregion
}
```

---

#### IPermissionRepository.cs

```csharp
namespace Identity.Domain.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);
    
    #region Future Methods - Phase 2
    // TODO Phase 2: Task<IEnumerable<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<Permission>> GetPermissionTreeAsync(CancellationToken cancellationToken = default);
    #endregion
}
```

---

#### IRefreshTokenRepository.cs

```csharp
namespace Identity.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    #region Future Methods - Phase 3
    // TODO Phase 3: Task<IEnumerable<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<RefreshToken>> GetTokenFamilyAsync(Guid tokenId, CancellationToken cancellationToken = default);
    #endregion
}
```

---

### 1.8: Domain Service Interfaces (15 minutes)

**Status**: ✅ Completed

**Location**: `Identity.Domain/Services/`

#### IPasswordHasher.cs

```csharp
namespace Identity.Domain.Services;

/// <summary>
/// Service for hashing and verifying passwords
/// </summary>
/// <remarks>
/// Implementation should be in Infrastructure layer using BCrypt or Argon2
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password
    /// </summary>
    string Hash(string password);
    
    /// <summary>
    /// Verify a plain text password against a hash
    /// </summary>
    bool Verify(string password, string hash);
    
    #region Future Methods - Phase 3
    // TODO Phase 3: string HashWithAlgorithm(string password, HashAlgorithm algorithm);
    // TODO Phase 3: bool NeedsRehash(string hash);
    #endregion
}
```

---

### 1.9: Project File Setup (10 minutes)

**Status**: ⚠️ Needs Update - Missing BuildingBlocks.Infrastructure reference

**File**: `Identity.Domain/Identity.Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Datarizen.Identity.Domain</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\BuildingBlocks.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Tasks**:
- [ ] Verify project file exists
- [ ] Verify BuildingBlocks.Kernel reference
- [ ] **Add BuildingBlocks.Infrastructure reference** (needed for `IRepository<TEntity, TKey>`)
- [ ] Build project: `dotnet build Identity.Domain.csproj`
- [ ] Verify zero compilation errors

**Note**: Domain layer needs BuildingBlocks.Infrastructure reference for the `IRepository<TEntity, TKey>` interface. This is acceptable because:
- Repository interfaces are part of the domain contract (what data access is needed)
- The interface itself has no EF Core dependencies
- Actual implementation (with EF Core) lives in Infrastructure layer
- This follows the Dependency Inversion Principle

---

### 1.10: Documentation (1 hour)

**Status**: ✅ Completed

#### Create Module README.md

**File**: `server/src/Product/Identity/README.md`

**Sections**:
1. **Overview**: What the Identity module does
2. **Domain Model**: Current entities and relationships (include diagram)
3. **Current Features**: What works now (basic CRUD)
4. **Feature Roadmap**:
   - Phase 1: Basic CRUD (current) - 9 hours
   - Phase 2: Security Hardening - 12 hours (email confirmation, account locking, password policies)
   - Phase 3: Advanced Features - 16 hours (MFA, external auth, sessions, GDPR)
   - Phase 4: Enterprise Scale - 8 hours (password expiration, data retention, anonymization)
5. **Extension Points**: Where to add complexity
6. **Dependencies**: BuildingBlocks.Kernel

---

#### Create Domain README.md

**File**: `server/src/Product/Identity/Identity.Domain/README.md`

**Sections**:
1. **Current Architecture**:
   - Entities (7): User, Credential, Role, Permission, UserRole, RolePermission, RefreshToken
   - Value Objects (2): Email, PasswordHash
   - Domain Events (10): List all events
   - Repository Interfaces (4): List all repositories
   - Domain Services (1): IPasswordHasher
   - Enums (1): CredentialType

2. **Planned Architecture**:
   - Future Value Objects: PasswordPolicy, ConsentVersion, IpAddress, EncryptedValue<T>, DataRetentionPolicy
   - Future Domain Services: IPasswordPolicyValidator, IPermissionChecker, IAccountLockoutPolicy, IDataAnonymizer
   - Future Specifications: ActiveUsersSpecification, UsersWithExpiredPasswordsSpecification, LockedAccountsSpecification
   - Future Entities: PasswordHistory, LoginAttempt, UserConsent, Session

3. **Migration Guide**:
   - How to add new properties to existing entities
   - How to add new value objects
   - How to add new domain services
   - How to maintain backward compatibility

4. **Design Decisions**:
   - Why Result<T> pattern
   - Why domain events
   - Why Guard clauses
   - Why IDateTimeProvider

---

#### Create Entity README.md Files

**Files**:
- `Identity.Domain/Entities/README.md` (overview of all entities)
- `Identity.Domain/Entities/User.README.md` (detailed User entity docs)
- `Identity.Domain/Entities/Role.README.md` (detailed Role entity docs)
- `Identity.Domain/Entities/Permission.README.md` (detailed Permission entity docs)

**Structure** (example for User.README.md):

```markdown
# User Entity

## Purpose
Represents a user account in the system. Aggregate root for user-related operations.

## Current Implementation (Phase 1)

### Properties
- `Id` (Guid): Unique identifier (inherited from AggregateRoot<Guid>)
- `DefaultTenantId` (Guid): Primary tenant context when user belongs to multiple tenants
- `Email` (Email): User's email address (value object)
- `DisplayName` (string): User's display name
- `IsActive` (bool): Whether user account is active
- `CreatedAt` (DateTime): When user was created (inherited from Entity<Guid>)
- `UpdatedAt` (DateTime?): When user was last updated (inherited from Entity<Guid>)

### Methods
- `Create()`: Factory method to create new user
- `Update()`: Update display name
- `Deactivate()`: Deactivate user account

### Domain Events
- `UserCreatedEvent`: Raised when user is created
- `UserUpdatedEvent`: Raised when user is updated
- `UserDeactivatedEvent`: Raised when user is deactivated

## Planned Enhancements

### Phase 2: Security Hardening (12 hours)
**Properties**:
- `EmailConfirmed` (bool): Whether email is confirmed
- `IsLocked` (bool): Whether account is locked
- `LockoutEnd` (DateTime?): When lockout expires
- `FailedLoginAttempts` (int): Number of failed login attempts

**Methods**:
- `ConfirmEmail()`: Confirm user's email address
- `Lock(reason, duration)`: Lock user account
- `Unlock()`: Unlock user account
- `RecordFailedLogin()`: Record failed login attempt
- `ResetFailedLoginAttempts()`: Reset failed login counter

**Events**:
- `EmailConfirmedEvent`
- `UserLockedEvent`
- `UserUnlockedEvent`
- `FailedLoginRecordedEvent`

### Phase 3: Advanced Features (16 hours)
**Properties**:
- `LastPasswordChangedAt` (DateTime?): When password was last changed
- `PasswordExpiresAt` (DateTime?): When password expires
- `MustChangePassword` (bool): Whether user must change password on next login

**Methods**:
- `ChangePassword()`: Change user password
- `RequirePasswordChange()`: Force password change on next login

**Events**:
- `PasswordChangedEvent`
- `PasswordChangeRequiredEvent`

### Phase 4: Enterprise Scale (8 hours)
**Properties**:
- `DeletedAt` (DateTime?): Soft delete timestamp
- `RowVersion` (byte[]): Optimistic concurrency token

**Methods**:
- `MarkForDeletion()`: Soft delete user
- `AnonymizePersonalData()`: GDPR anonymization

**Events**:
- `UserMarkedForDeletionEvent`
- `UserAnonymizedEvent`

## Business Rules

### Current Invariants (Phase 1)
- Email must be unique per tenant
- DisplayName cannot be empty
- User must have at least one credential (enforced by application layer)

### Future Invariants
- Cannot authenticate if locked or email not confirmed (Phase 2)
- Account locks after N failed login attempts (Phase 2)
- Password cannot be in history (Phase 3)
- Consent must be current version (Phase 3)

## Usage Examples

### Create User
```csharp
var emailResult = Email.Create("user@example.com");
if (emailResult.IsFailure)
    return emailResult.Error;

var userResult = User.Create(
    tenantId,
    emailResult.Value,
    "John Doe",
    dateTimeProvider
);
if (userResult.IsFailure)
    return userResult.Error;

await _userRepository.AddAsync(userResult.Value);
```

### Update User
```csharp
var user = await _userRepository.GetByIdAsync(userId);
if (user is null)
    return Error.NotFound("User not found");

var updateResult = user.Update("Jane Doe", dateTimeProvider);
if (updateResult.IsFailure)
    return updateResult.Error;

await _userRepository.UpdateAsync(user);
```

### Deactivate User
```csharp
var user = await _userRepository.GetByIdAsync(userId);
if (user is null)
    return Error.NotFound("User not found");

var deactivateResult = user.Deactivate(dateTimeProvider);
if (deactivateResult.IsFailure)
    return deactivateResult.Error;

await _userRepository.UpdateAsync(user);
```

## Testing Considerations

### Unit Tests
- Test factory method validation
- Test state transitions (active → deactivated)
- Test domain event raising
- Test business rule enforcement

### Integration Tests
- Test repository operations
- Test with real database
- Test concurrent updates (when RowVersion added in Phase 4)

## Migration Path

### Adding New Properties (Example: EmailConfirmed in Phase 2)

1. Add property to entity:
```csharp
public bool EmailConfirmed { get; private set; }
```

2. Update factory method:
```csharp
EmailConfirmed = false, // Default value
```

3. Add method to change state:
```csharp
public Result ConfirmEmail(IDateTimeProvider dateTimeProvider)
{
    if (EmailConfirmed)
        return Result.Failure("Email is already confirmed");
    
    EmailConfirmed = true;
    UpdatedAt = dateTimeProvider.UtcNow;
    
    RaiseDomainEvent(new EmailConfirmedEvent(Id, dateTimeProvider.UtcNow));
    
    return Result.Success();
}
```

4. Create migration to add column
5. Update EF Core configuration
6. Update application layer commands/queries
7. Update API endpoints
```

---

**Deliverable**: Complete domain layer with minimal viable entities, enterprise patterns, comprehensive documentation, and clear roadmap for future enhancements.

---

## Phase 2: Application Layer (Future)

**Status**: ⏭️ Next (not yet defined in this plan)

**Objective**: Implement MediatR command/query handlers, DTOs, validators, and application services.

**Estimated Effort**: 12 hours

**Details**: This plan does not specify Phase 2 tasks yet. Next step is to author a dedicated `Identity.Application` plan (commands/queries/handlers/validators/contracts).

---

## Phase 3: Infrastructure Layer (Future)

**Status**: 📋 Planned

**Objective**: Implement DbContext, repositories, EF Core configurations, and FluentMigrator migrations.

**Estimated Effort**: 10 hours

**Details**: To be planned after Phase 2 completion.

---

## Phase 4: API Layer (Future)

**Status**: 📋 Planned

**Objective**: Replace stub controllers with real implementations using MediatR.

**Estimated Effort**: 6 hours

**Details**: To be planned after Phase 3 completion.

---

## Success Criteria

### Phase 1 (Domain Layer)
- ✅ All base classes exist in BuildingBlocks.Kernel (Entity, AggregateRoot, ValueObject, DomainEvent, Result<T>)
- ✅ Guard class with validation methods
- ✅ IDateTimeProvider interface and implementation
- ✅ All 7 entities compile without errors
- ✅ All 2 value objects with proper validation
- ✅ All 10 domain events as record types
- ✅ All 4 repository interfaces
- ✅ IPasswordHasher domain service interface
- ✅ Zero dependencies on Infrastructure/Application layers
- ✅ All factory methods return Result<T>
- ✅ Domain events raised for all state changes
- ✅ Guard clauses used for validation
- ✅ XML documentation on all public APIs
- ✅ Module README.md documents roadmap
- ✅ Domain README.md documents architecture
- ✅ Entity README.md files document enhancements
- ✅ All future enhancements documented in code comments with phase numbers

### Phase 1 Should NOT Have
- ❌ EF Core references
- ❌ MediatR references
- ❌ ASP.NET Core references
- ❌ DTOs (those are in Application/Contracts)
- ❌ Validation attributes (use FluentValidation in Application)
- ❌ Public setters (use methods for state changes)
- ❌ Parameterless constructors (except private for EF Core)

---

## Next Steps After Phase 1

1. **Review Domain Model**: Validate entities, value objects, events with team
2. **Plan Application Layer**: Design command/query handlers, DTOs, validators
3. **Plan Infrastructure Layer**: Design DbContext, repositories, migrations
4. **Plan API Layer**: Design controller endpoints, request/response DTOs

---

## Estimated Timeline

- **Phase 1 (Domain)**: 9 hours (1.1 days)
- **Phase 2 (Application)**: 12 hours (1.5 days)
- **Phase 3 (Infrastructure)**: 10 hours (1.25 days)
- **Phase 4 (API)**: 6 hours (0.75 days)

**Total First Iteration**: ~37 hours (~4.6 days)

**Future Enhancements**:
- Phase 2 (Security Hardening): ~12 hours
- Phase 3 (Advanced Features): ~16 hours
- Phase 4 (Enterprise Scale): ~8 hours

**Total with All Enhancements**: ~73 hours (~9 days)

### Additional Base Patterns

#### 1. Base Entity (Already Exists in BuildingBlocks.Kernel)

All entities should inherit from `Entity<TId>`:

```csharp
// From BuildingBlocks.Kernel
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent) { }
    public void ClearDomainEvents() { }
}
```

**Usage**:
```csharp
public class User : Entity<Guid>
{
    // No need to redefine Id, CreatedAt, UpdatedAt, domain events
}
```

---

#### 2. Base Value Object (Already Exists in BuildingBlocks.Kernel)

All value objects should inherit from `ValueObject`:

```csharp
// From BuildingBlocks.Kernel
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    public override bool Equals(object? obj) { }
    public override int GetHashCode() { }
    public static bool operator ==(ValueObject? left, ValueObject? right) { }
    public static bool operator !=(ValueObject? left, ValueObject? right) { }
}
```

**Usage**:
```csharp
public class Email : ValueObject
{
    public string Value { get; }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

---

#### 3. Base Domain Event (Already Exists in BuildingBlocks.Kernel)

All domain events should inherit from `DomainEvent`:

```csharp
// From BuildingBlocks.Kernel
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

**Usage**:
```csharp
public record UserCreatedEvent(
    Guid UserId,
    Guid TenantId,
    string Email,
    DateTime CreatedAt) : DomainEvent;
```

---

#### 4. Base Command Handler (Already Exists in BuildingBlocks.Kernel)

All command handlers should implement `ICommandHandler<TCommand, TResponse>`:

```csharp
// From BuildingBlocks.Kernel
public interface ICommandHandler<in TCommand, TResponse> 
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
```

**Usage**:
```csharp
public class CreateUserCommandHandler 
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

---

#### 5. Base Query Handler (Already Exists in BuildingBlocks.Kernel)

All query handlers should implement `IQueryHandler<TQuery, TResponse>`:

```csharp
// From BuildingBlocks.Kernel
public interface IQueryHandler<in TQuery, TResponse> 
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
```

**Usage**:
```csharp
public class GetUserByIdQueryHandler 
    : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<Result<UserDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

---

#### 6. Base Repository Implementation (Create in BuildingBlocks.Infrastructure)

**Action Required**: Verify `Repository<TEntity, TKey>` exists in BuildingBlocks.Infrastructure:

```csharp
// BuildingBlocks.Infrastructure/Persistence/Repository.cs
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    protected readonly DbContext Context;
    protected readonly DbSet<TEntity> DbSet;
    
    public Repository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }
    
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync(new object[] { id }, cancellationToken);
    
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await DbSet.ToListAsync(cancellationToken);
    
    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(cancellationToken);
    
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(predicate, cancellationToken);
    
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);
    
    public virtual void Update(TEntity entity)
        => DbSet.Update(entity);
    
    public virtual void Delete(TEntity entity)
        => DbSet.Remove(entity);
}
```

**Custom Repository Implementation**:
```csharp
// Identity.Infrastructure/Persistence/Repositories/UserRepository.cs
public class UserRepository : Repository<User, Guid>, IUserRepository
{
    public UserRepository(IdentityDbContext context) : base(context)
    {
    }
    
    // Only implement domain-specific methods
    public async Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                u => u.Email == email,
                cancellationToken);
    }
    
    public async Task<bool> EmailExistsAsync(
        Email email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(
                u => u.Email == email,
                cancellationToken);
    }
}
```

---




