# Identity Module - Phase 1 Domain Layer Refactoring Plan

**Status**: ✅ Implemented (Domain refactored; optional Guard Result-returning methods and Phase 4 tests not done)  
**Last Updated**: 2025-02-07  
**Estimated Total Time**: ~14 hours

---

## Overview

This document outlines the refactoring required to bring the **Phase 1: Identity.Domain** implementation into compliance with enterprise best practices as documented in `module-identity-domain-layer-plan.md`.

**Current Status**: Phase 1 entities have been refactored. All 7 entities use `Result<T>`/`Result`, `IDateTimeProvider`, value objects where specified, guard-style validation, XML documentation, and future-enhancement comments. Repositories inherit specification support from `IRepository<T, TKey>`.

**Goal**: Refactor all 7 entities to follow:
1. ✅ Factory methods return `Result<T>`
2. ✅ State-changing methods return `Result` or `Result<T>`
3. ✅ Guard clauses for all inputs
4. ✅ XML documentation on all public APIs
5. ✅ `IDateTimeProvider` injection (no `DateTime.UtcNow`)
6. ✅ Value objects used in entities
7. ✅ Future enhancement documentation
8. ✅ Repository specification support

---

## Critical Issues Summary

| Issue | Severity | Entities Affected | Effort |
|-------|----------|-------------------|--------|
| Factory methods not returning `Result<T>` | 🔴 Critical | All 7 entities | 2 hours |
| State methods throwing exceptions | 🔴 Critical | All 7 entities | 3 hours |
| Missing Guard clauses | 🔴 Critical | All 7 entities | 2 hours |
| Missing XML documentation | 🟡 High | All entities/methods | 3 hours |
| Using `DateTime.UtcNow` directly | 🟡 High | All 7 entities | 1.5 hours |
| Value objects not used in entities | 🟡 High | `User`, `Credential` | 1 hour |
| Missing future enhancement docs | 🟢 Medium | All 7 entities | 1 hour |
| Repository missing spec support | 🟢 Medium | All 4 repositories | 30 minutes |

**Total Effort**: ~14 hours (~1.75 days)

---

## Phase 1: Critical Refactorings (7 hours)

### 1.1: Refactor User Entity (2 hours)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/User.cs`

#### 1.1.1: Use Value Objects (15 minutes)

**Current**:
```csharp
public string Email { get; private set; } = string.Empty; // ❌ Primitive obsession
```

**Refactor**:
```csharp
public Email Email { get; private set; } = null!; // ✅ Value object
```

**Tasks**:
- [x] Replace `string Email` with `Email Email`
- [x] Update all constructors/factory methods
- [x] Update all domain events to use `Email.Value`

---

#### 1.1.2: Factory Method Returns `Result<User>` (30 minutes)

**Current**:
```csharp
public static User Create(string email, string passwordHash, ...)
{
    var user = new User { ... };
    return user; // ❌ Returns User directly
}
```

**Refactor**:
```csharp
/// <summary>
/// Creates a new user account.
/// </summary>
/// <param name="defaultTenantId">Default tenant ID for the user</param>
/// <param name="email">User's email address (unique identifier)</param>
/// <param name="displayName">User's display name</param>
/// <param name="dateTimeProvider">Date/time provider for timestamps</param>
/// <returns>Result containing the created user or validation errors</returns>
public static Result<User> Create(
    Guid defaultTenantId,
    Email email,
    string displayName,
    IDateTimeProvider dateTimeProvider)
{
    // Guard clauses
    var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
    if (displayNameResult.IsFailure)
        return Result<User>.Failure(displayNameResult.Error);

    ArgumentNullException.ThrowIfNull(email);
    ArgumentNullException.ThrowIfNull(dateTimeProvider);

    var now = dateTimeProvider.UtcNow;

    var user = new User
    {
        Id = Guid.NewGuid(),
        DefaultTenantId = defaultTenantId,
        Email = email,
        DisplayName = displayName,
        IsActive = true,
        CreatedAt = now
    };

    user.RaiseDomainEvent(new UserCreatedEvent(
        user.Id,
        user.DefaultTenantId,
        user.Email.Value,
        now));

    return Result<User>.Success(user);
}
```

**Tasks**:
- [x] Change return type from `User` to `Result<User>`
- [x] Add Guard clauses for all string parameters
- [x] Add `ArgumentNullException.ThrowIfNull` for reference types
- [x] Inject `IDateTimeProvider` parameter
- [x] Replace `DateTime.UtcNow` with `dateTimeProvider.UtcNow`
- [x] Add XML documentation
- [x] Update all callers (command handlers)

---

#### 1.1.3: State Methods Return `Result` (45 minutes)

**Current**:
```csharp
public void ConfirmEmail()
{
    if (IsEmailConfirmed)
        throw new InvalidOperationException("Email already confirmed"); // ❌ Throws exception
    
    IsEmailConfirmed = true;
    EmailConfirmedAt = DateTime.UtcNow; // ❌ Direct DateTime usage
}
```

**Refactor**:
```csharp
/// <summary>
/// Confirms the user's email address.
/// </summary>
/// <param name="dateTimeProvider">Date/time provider for timestamps</param>
/// <returns>Success if email confirmed; Failure if already confirmed</returns>
public Result ConfirmEmail(IDateTimeProvider dateTimeProvider)
{
    ArgumentNullException.ThrowIfNull(dateTimeProvider);

    if (IsEmailConfirmed)
        return Result.Failure(Error.Validation(
            "User.EmailAlreadyConfirmed",
            "Email is already confirmed"));

    IsEmailConfirmed = true;
    EmailConfirmedAt = dateTimeProvider.UtcNow;
    
    RaiseDomainEvent(new UserEmailConfirmedEvent(Id, Email.Value, dateTimeProvider.UtcNow));
    
    return Result.Success();
}
```

**Methods to Refactor**:
- [ ] `ConfirmEmail()` → `ConfirmEmail(IDateTimeProvider)` (Phase 2 – not yet on User)
- [x] `Deactivate()` → `Deactivate(IDateTimeProvider)`
- [ ] `Activate()` → `Activate(IDateTimeProvider)` (Phase 2 – not yet on User)
- [ ] `RecordLogin()` → `RecordLogin(IDateTimeProvider)` (Phase 2 – not yet on User)
- [x] `Update(...)` → `Update(..., IDateTimeProvider)` (implemented as Update(displayName))

**Tasks**:
- [x] Change return type from `void` to `Result`
- [x] Replace exceptions with `Result.Failure(...)`
- [x] Inject `IDateTimeProvider` parameter
- [x] Replace `DateTime.UtcNow` with `dateTimeProvider.UtcNow`
- [x] Add XML documentation
- [x] Update all callers (command handlers)

---

#### 1.1.4: Add Future Enhancement Documentation (15 minutes)

**Refactor**:
```csharp
public class User : AggregateRoot<Guid>
{
    // ===== Phase 1: Minimal Viable Properties =====
    public Guid DefaultTenantId { get; private set; }
    public Email Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // ===== Phase 2: Security Hardening (Future) =====
    // TODO Phase 2: Add email confirmation token
    // public string? EmailConfirmationToken { get; private set; }
    // public DateTime? EmailConfirmationTokenExpiresAt { get; private set; }
    
    // TODO Phase 2: Add account lockout
    // public bool IsLockedOut { get; private set; }
    // public DateTime? LockedOutUntil { get; private set; }
    // public int FailedLoginAttempts { get; private set; }
    
    // TODO Phase 2: Add password policies
    // public DateTime? PasswordChangedAt { get; private set; }
    // public bool MustChangePassword { get; private set; }

    // ===== Phase 3: Advanced Features (Future) =====
    // TODO Phase 3: Add MFA
    // public bool IsMfaEnabled { get; private set; }
    // public string? MfaSecret { get; private set; }
    
    // TODO Phase 3: Add external auth
    // public ICollection<ExternalLogin> ExternalLogins { get; private set; }
    
    // TODO Phase 3: Add sessions
    // public ICollection<UserSession> Sessions { get; private set; }

    // ===== Phase 4: Enterprise Scale (Future) =====
    // TODO Phase 4: Add password expiration
    // public int PasswordExpirationDays { get; private set; }
    
    // TODO Phase 4: Add data retention
    // public DateTime? DataRetentionExpiresAt { get; private set; }
    
    // TODO Phase 4: Add anonymization
    // public bool IsAnonymized { get; private set; }
    // public DateTime? AnonymizedAt { get; private set; }
}
```

**Tasks**:
- [x] Add phase comments to all properties
- [x] Add TODO comments for future enhancements
- [x] Reference `module-identity-domain-layer-plan.md` phases

---

### 1.2: Refactor Role Entity (1.5 hours)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/Role.cs`

**Tasks**:
- [x] Factory method returns `Result<Role>`
- [x] Add Guard clauses
- [x] Inject `IDateTimeProvider`
- [ ] `AddPermission()` returns `Result` (Phase 2 – not yet on Role)
- [ ] `RemovePermission()` returns `Result` (Phase 2 – not yet on Role)
- [x] Add XML documentation
- [x] Add future enhancement comments

**Example**:
```csharp
/// <summary>
/// Creates a new role.
/// </summary>
public static Result<Role> Create(
    string code,
    string name,
    string description,
    IDateTimeProvider dateTimeProvider)
{
    var codeResult = Guard.Against.NullOrWhiteSpace(code, nameof(code));
    if (codeResult.IsFailure) return Result<Role>.Failure(codeResult.Error);

    var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    if (nameResult.IsFailure) return Result<Role>.Failure(nameResult.Error);

    ArgumentNullException.ThrowIfNull(dateTimeProvider);

    var role = new Role
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = name,
        Description = description,
        IsActive = true,
        CreatedAt = dateTimeProvider.UtcNow
    };

    role.RaiseDomainEvent(new RoleCreatedEvent(role.Id, role.Code, role.Name));
    return Result<Role>.Success(role);
}

/// <summary>
/// Adds a permission to this role.
/// </summary>
public Result AddPermission(Guid permissionId, IDateTimeProvider dateTimeProvider)
{
    ArgumentNullException.ThrowIfNull(dateTimeProvider);

    if (_permissions.Any(p => p.PermissionId == permissionId))
        return Result.Failure(Error.Validation(
            "Role.PermissionAlreadyExists",
            $"Permission {permissionId} already assigned to role {Code}"));

    var rolePermission = new RolePermission
    {
        RoleId = Id,
        PermissionId = permissionId,
        GrantedAt = dateTimeProvider.UtcNow
    };

    _permissions.Add(rolePermission);
    RaiseDomainEvent(new PermissionGrantedToRoleEvent(Id, permissionId));
    
    return Result.Success();
}
```

---

### 1.3: Refactor Permission Entity (1 hour)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/Permission.cs`

**Tasks**:
- [x] Factory method returns `Result<Permission>`
- [x] Add Guard clauses
- [x] Inject `IDateTimeProvider`
- [x] Add XML documentation
- [x] Add future enhancement comments

---

### 1.4: Refactor Credential Entity (1 hour)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/Credential.cs`

**Tasks**:
- [x] Use `PasswordHash` value object instead of `string`
- [x] Factory method returns `Result<Credential>`
- [x] Add Guard clauses
- [x] Inject `IDateTimeProvider`
- [x] `UpdatePassword()` returns `Result`
- [x] Add XML documentation

**Example**:
```csharp
public class Credential : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public PasswordHash PasswordHash { get; private set; } = null!; // ✅ Value object
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static Result<Credential> Create(
        Guid userId,
        PasswordHash passwordHash,
        IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = passwordHash,
            CreatedAt = dateTimeProvider.UtcNow
        };

        return Result<Credential>.Success(credential);
    }

    public Result UpdatePassword(
        PasswordHash newPasswordHash,
        IDateTimeProvider dateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(newPasswordHash);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);

        PasswordHash = newPasswordHash;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result.Success();
    }
}
```

---

### 1.5: Refactor RefreshToken Entity (45 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/RefreshToken.cs`

**Status**: ✅ Already uses `IDateTimeProvider` and `Result<T>`

**Tasks**:
- [x] Add Guard clauses (currently uses `ArgumentNullException.ThrowIfNull` and validation returns)
- [x] Add XML documentation
- [x] Verify all methods return `Result`

---

### 1.6: Refactor UserRole Entity (30 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/UserRole.cs`

**Tasks**:
- [x] Factory method returns `Result<UserRole>`
- [x] Inject `IDateTimeProvider`
- [x] Add XML documentation

---

### 1.7: Refactor RolePermission Entity (30 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/RolePermission.cs`

**Tasks**:
- [x] Factory method returns `Result<RolePermission>`
- [x] Inject `IDateTimeProvider`
- [x] Add XML documentation

---

## Phase 2: High Priority Refactorings (4.5 hours)

### 2.1: Add XML Documentation (3 hours)

**Scope**: All public APIs in all 7 entities

**Template**:
```csharp
/// <summary>
/// [Brief description of class/method/property]
/// </summary>
/// <param name="paramName">[Parameter description]</param>
/// <returns>[Return value description]</returns>
/// <exception cref="ArgumentNullException">Thrown when [condition]</exception>
```

**Tasks**:
- [x] Document all 7 entity classes
- [x] Document all factory methods
- [x] Document all state-changing methods
- [x] Document all properties
- [ ] Document all domain events (events have minimal docs; add if needed)

**Estimated Time**:
- User: 45 minutes
- Role: 30 minutes
- Permission: 20 minutes
- Credential: 20 minutes
- RefreshToken: 20 minutes
- UserRole: 15 minutes
- RolePermission: 15 minutes

---

### 2.2: Update Command Handlers (1.5 hours)

**Location**: `Identity.Application/Commands/`

**Impact**: All command handlers must handle `Result<T>` from factory methods

**Current**:
```csharp
public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    var user = User.Create(request.Email, passwordHash, ...); // ❌ Returns User
    await _userRepository.AddAsync(user, cancellationToken);
    return Result<Guid>.Success(user.Id);
}
```

**Refactor**:
```csharp
public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    var userResult = User.Create(
        request.DefaultTenantId,
        Email.Create(request.Email).Value, // Email value object
        request.DisplayName,
        _dateTimeProvider); // ✅ Returns Result<User>

    if (userResult.IsFailure)
        return Result<Guid>.Failure(userResult.Error);

    var user = userResult.Value;
    await _userRepository.AddAsync(user, cancellationToken);
    
    return Result<Guid>.Success(user.Id);
}
```

**Tasks**:
- [x] Update `CreateUserCommandHandler`
- [x] Update `UpdateUserCommandHandler`
- [ ] Update `CreateRoleCommandHandler` (when added)
- [ ] Update `CreatePermissionCommandHandler` (when added)
- [x] Update all handlers that call factory methods
- [x] Inject `IDateTimeProvider` into all handlers

---

## Phase 3: Medium Priority Refactorings (2 hours)

### 3.1: Add Repository Specification Support (30 minutes)

**Files**:
- `Identity.Domain/Repositories/IUserRepository.cs`
- `Identity.Domain/Repositories/IRoleRepository.cs`
- `Identity.Domain/Repositories/IPermissionRepository.cs`
- `Identity.Domain/Repositories/IRefreshTokenRepository.cs`

**Current**:
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    // ❌ No specification methods
}
```

**Refactor**:
```csharp
using Ardalis.Specification;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    // ✅ Specification pattern support
    Task<User?> GetBySpecAsync(ISpecification<User> spec, CancellationToken cancellationToken = default);
    Task<List<User>> ListAsync(ISpecification<User> spec, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<User> spec, CancellationToken cancellationToken = default);
}
```

**Tasks**:
- [x] Add specification methods to all 4 repository interfaces (inherited from `IRepository<TEntity, TKey>`: `FirstOrDefaultAsync(ISpecification)`, `ListAsync(ISpecification)`, `CountAsync(ISpecification)`)
- [x] Update repository implementations in `Identity.Infrastructure` (use base `Repository<T, TKey>`)
- [x] Update query handlers to use specifications

---

### 3.2: Create Guard Extension Methods (1 hour)

**File**: `BuildingBlocks.Kernel/Domain/Guard.cs`

**Add Missing Methods**:
```csharp
public static class Guard
{
    public static Result Against.NullOrWhiteSpace(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure(Error.Validation(
                $"{paramName}.NullOrWhiteSpace",
                $"{paramName} cannot be null or whitespace"));
        
        return Result.Success();
    }

    public static Result Against.InvalidEmail(string email)
    {
        if (!IsValidEmail(email))
            return Result.Failure(Error.Validation(
                "Email.Invalid",
                "Email address is not in a valid format"));
        
        return Result.Success();
    }

    public static Result Against.OutOfRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            return Result.Failure(Error.Validation(
                $"{paramName}.OutOfRange",
                $"{paramName} must be between {min} and {max}"));
        
        return Result.Success();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

**Tasks**:
- [ ] Add `Against.NullOrWhiteSpace` (optional; entities use manual validation and `Error.Validation` today)
- [x] Add `Against.InvalidEmail` (Kernel has `Guard.Against.InvalidEmail` – throw-based)
- [x] Add `Against.OutOfRange` (Kernel has `Guard.Against.OutOfRange` – throw-based)
- [ ] Add unit tests for all Guard methods

**Note**: Plan suggested Result-returning Guard methods; implementation uses throw-based Guard in Kernel and manual `Result.Failure(Error.Validation(...))` in entities.

---

### 3.3: Add Future Enhancement Documentation (30 minutes)

**Tasks**:
- [x] Add phase comments to all entities (see 1.1.4)
- [x] Create `Identity.Domain/README.md` with roadmap (exists)
- [ ] Document extension points in README (optional)

---

## Phase 4: Testing & Validation (2 hours)

### 4.1: Update Unit Tests (1.5 hours)

**Location**: `Identity.Domain.Tests/`

**Tasks**:
- [ ] Update all entity tests to handle `Result<T>`
- [ ] Add tests for Guard clause failures
- [ ] Add tests for `IDateTimeProvider` injection
- [ ] Add tests for value object usage
- [ ] Verify >80% code coverage

**Example**:
```csharp
[Fact]
public void Create_WithValidData_ReturnsSuccess()
{
    // Arrange
    var email = Email.Create("test@example.com").Value;
    var dateTimeProvider = new FakeDateTimeProvider(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));

    // Act
    var result = User.Create(
        Guid.NewGuid(),
        email,
        "John Doe",
        dateTimeProvider);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Email.Should().Be(email);
    result.Value.CreatedAt.Should().Be(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
}

[Fact]
public void Create_WithNullDisplayName_ReturnsFailure()
{
    // Arrange
    var email = Email.Create("test@example.com").Value;
    var dateTimeProvider = new FakeDateTimeProvider();

    // Act
    var result = User.Create(
        Guid.NewGuid(),
        email,
        null!, // ❌ Invalid
        dateTimeProvider);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("displayName.NullOrWhiteSpace");
}
```

---

### 4.2: Integration Tests (30 minutes)

**Tasks**:
- [ ] Verify command handlers work with refactored entities
- [ ] Verify repository specification methods work
- [ ] Verify domain events are raised correctly

---

## Success Criteria

### Phase 1 (Critical):
- ✅ All factory methods return `Result<T>`
- ✅ All state-changing methods return `Result` or `Result<T>`
- ✅ All inputs validated with Guard clauses
- ✅ No `DateTime.UtcNow` usage (all use `IDateTimeProvider`)
- ✅ Value objects used in `User` and `Credential`

### Phase 2 (High Priority):
- ✅ All public APIs have XML documentation
- ✅ All command handlers updated to handle `Result<T>`

### Phase 3 (Medium Priority):
- ✅ All repositories support Ardalis.Specification
- ✅ Guard extension methods created
- ✅ Future enhancement documentation added

### Phase 4 (Testing):
- ✅ All unit tests passing
- ✅ >80% code coverage
- ✅ Integration tests passing

---

## Estimated Timeline

| Phase | Tasks | Effort |
|-------|-------|--------|
| **Phase 1: Critical Refactorings** | 7 entities | 7 hours |
| **Phase 2: High Priority** | XML docs + handlers | 4.5 hours |
| **Phase 3: Medium Priority** | Repos + Guard + docs | 2 hours |
| **Phase 4: Testing** | Unit + integration tests | 2 hours |
| **Total** | | **~15.5 hours (~2 days)** |

---

## Migration Strategy

### Step 1: Create Feature Branch
```bash
git checkout -b refactor/identity-domain-enterprise-patterns
```

### Step 2: Refactor Entities (One at a Time)
1. Start with `RefreshToken` (already partially compliant)
2. Then `Permission` (simplest)
3. Then `Role`
4. Then `User` (most complex)
5. Then `Credential`
6. Then `UserRole` and `RolePermission`

### Step 3: Update Command Handlers
- Update all handlers after entities are refactored

### Step 4: Update Tests
- Update unit tests after each entity
- Run full test suite before merging

### Step 5: Code Review
- Review all changes against this plan
- Verify all success criteria met

### Step 6: Merge to Main
```bash
git merge refactor/identity-domain-enterprise-patterns
```

---

## Next Steps After Completion

1. **Phase 0: BuildingBlocks Enhancement**
   - Follow `module-identity-application-layer-plan-refactoring.md`
   - Implement missing abstractions and capabilities

2. **Phase 2: Identity.Application**
   - Verify all command/query handlers work with refactored domain
   - Add missing validators
   - Add missing specifications

3. **Phase 3: Identity.Infrastructure**
   - Implement repository specification methods
   - Add EF Core configurations for value objects
   - Create migrations

4. **Phase 4: Identity.Api**
   - Update controllers to handle `Result<T>`
   - Add proper error responses
   - Update Swagger documentation

---

## Notes

- **Backward Compatibility**: This refactoring BREAKS existing command handlers. All handlers must be updated.
- **Testing**: Run full test suite after each entity refactoring.
- **Code Review**: Mandatory review before merging.
- **Documentation**: Update all README files after completion.
- **Performance**: No performance impact expected (Result<T> is a struct).

---

## Questions for Review

1. ✅ Do we agree that all factory methods should return `Result<T>`?
2. ✅ Do we agree that all state-changing methods should return `Result`?
3. ✅ Do we agree that Guard clauses are mandatory for all inputs?
4. ✅ Do we agree that `IDateTimeProvider` injection is required?
5. ✅ Do we agree that value objects should be used for `Email` and `PasswordHash`?
6. ✅ Do we agree that XML documentation is mandatory for all public APIs?

**If all answers are YES, proceed with refactoring. If NO, discuss concerns.**

---

*End of Refactoring Plan*