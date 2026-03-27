# Identity Module - Phase 1 Domain Layer Fixes Plan

**Status**: ✅ Partially implemented (Guard clauses, Result extensions, ValidationBehavior already correct; EF config/migration and extra handlers when added)  
**Last Updated**: 2025-02-07  
**Estimated Total Time**: ~9 hours (excluding tests)

---

## Overview

This document outlines the fixes required to complete the **Phase 1: Identity.Domain** refactoring.

**Current Status**: Guard clauses (Result-returning), entity updates, and Result extensions are implemented. ValidationBehavior already handles `Result`/`Result<T>`. EF Core value object config and migrations apply when Identity.Infrastructure has DbContext/configurations.

**Goal**: Fix all remaining issues to achieve full enterprise compliance:
1. ✅ Consistent Guard clause usage (no exceptions thrown)
2. ✅ All command handlers updated for `Result<T>`
3. ✅ Complete Guard method library
4. ✅ ValidationBehavior handles `Result<T>` correctly
5. ✅ Repository implementations support specifications
6. ✅ EF Core value object configurations
7. ✅ Database migration for value objects
8. ✅ Domain events include timestamps
9. ✅ Result extension methods for cleaner code

---

## Critical Issues (Must Fix)

### Issue 1: Inconsistent Guard Clause Usage (1 hour) ✅ Done

**Problem**: Entities mix `ArgumentNullException.ThrowIfNull` (throws exception) with `Guard.Against.*` (returns `Result`).

**Current Code**:
```csharp
public static Result<User> Create(...)
{
    ArgumentNullException.ThrowIfNull(email); // ❌ Throws exception
    ArgumentNullException.ThrowIfNull(dateTimeProvider); // ❌ Throws exception
    
    var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName)); // ✅ Returns Result
    if (displayNameResult.IsFailure)
        return Result<User>.Failure(displayNameResult.Error);
}
```

**Fix Required**: Replace ALL `ArgumentNullException.ThrowIfNull` with `Guard.Against.Null`.

---

#### Step 1.1: Add `Guard.Against.Null` Method (15 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/Guard.cs`

**Action**: Add new method to Guard class.

```csharp
public static class Guard
{
    public static class Against
    {
        /// <summary>
        /// Guards against null reference types.
        /// </summary>
        /// <typeparam name="T">The reference type to check</typeparam>
        /// <param name="value">The value to check for null</param>
        /// <param name="paramName">The parameter name for error messages</param>
        /// <returns>Success if value is not null; Failure with validation error if null</returns>
        public static Result Null<T>(T? value, string paramName) where T : class
        {
            if (value is null)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.Null",
                    $"{paramName} cannot be null"));
            }
            
            return Result.Success();
        }

        // Existing method - keep as-is
        public static Result NullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.NullOrWhiteSpace",
                    $"{paramName} cannot be null or whitespace"));
            }
            
            return Result.Success();
        }
    }
}
```

**Validation**:
- Method signature matches pattern: `Result MethodName(T value, string paramName)`
- Returns `Result.Failure` with proper error code format: `{paramName}.{ErrorType}`
- XML documentation complete

---

#### Step 1.2: Update User Entity (10 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/User.cs`

**Action**: Replace `ArgumentNullException.ThrowIfNull` with `Guard.Against.Null` in `Create` method.

**Find**:
```csharp
public static Result<User> Create(
    Guid defaultTenantId,
    Email email,
    string displayName,
    IDateTimeProvider dateTimeProvider)
{
    ArgumentNullException.ThrowIfNull(email);
    ArgumentNullException.ThrowIfNull(dateTimeProvider);

    var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
    if (displayNameResult.IsFailure)
        return Result<User>.Failure(displayNameResult.Error);
```

**Replace With**:
```csharp
public static Result<User> Create(
    Guid defaultTenantId,
    Email email,
    string displayName,
    IDateTimeProvider dateTimeProvider)
{
    // Guard clauses - all return Result
    var emailResult = Guard.Against.Null(email, nameof(email));
    if (emailResult.IsFailure)
        return Result<User>.Failure(emailResult.Error);

    var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
    if (displayNameResult.IsFailure)
        return Result<User>.Failure(displayNameResult.Error);

    var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
    if (dateTimeProviderResult.IsFailure)
        return Result<User>.Failure(dateTimeProviderResult.Error);
```

**Also Update**: `Deactivate` and `Update` methods in same file.

**Find in `Deactivate`**:
```csharp
public Result Deactivate(IDateTimeProvider dateTimeProvider)
{
    ArgumentNullException.ThrowIfNull(dateTimeProvider);
```

**Replace With**:
```csharp
public Result Deactivate(IDateTimeProvider dateTimeProvider)
{
    var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
    if (dateTimeProviderResult.IsFailure)
        return dateTimeProviderResult;
```

**Find in `Update`**:
```csharp
public Result Update(string displayName)
{
    var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
```

**Keep as-is** (already correct).

---

#### Step 1.3: Update Role Entity (5 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/Role.cs`

**Action**: Replace `ArgumentNullException.ThrowIfNull` in `Create`, `AddPermission`, and `RemovePermission` methods.

**Pattern to Find**:
```csharp
ArgumentNullException.ThrowIfNull(dateTimeProvider);
```

**Replace With**:
```csharp
var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
if (dateTimeProviderResult.IsFailure)
    return Result<Role>.Failure(dateTimeProviderResult.Error); // Or Result.Failure for void methods
```

**Methods to Update**:
- `Create` (returns `Result<Role>`)
- `AddPermission` (returns `Result`)
- `RemovePermission` (returns `Result`)

---

#### Step 1.4: Update Permission Entity (5 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/Permission.cs`

**Action**: Replace `ArgumentNullException.ThrowIfNull` in `Create` method.

**Find**:
```csharp
public static Result<Permission> Create(...)
{
    ArgumentNullException.ThrowIfNull(dateTimeProvider);
```

**Replace With**:
```csharp
public static Result<Permission> Create(...)
{
    var codeResult = Guard.Against.NullOrWhiteSpace(code, nameof(code));
    if (codeResult.IsFailure)
        return Result<Permission>.Failure(codeResult.Error);

    var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    if (nameResult.IsFailure)
        return Result<Permission>.Failure(nameResult.Error);

    var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
    if (dateTimeProviderResult.IsFailure)
        return Result<Permission>.Failure(dateTimeProviderResult.Error);
```

---

#### Step 1.5: Update Credential Entity (5 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/Credential.cs`

**Action**: Replace `ArgumentNullException.ThrowIfNull` in `Create` and `UpdatePassword` methods.

**Find in `Create`**:
```csharp
ArgumentNullException.ThrowIfNull(passwordHash);
ArgumentNullException.ThrowIfNull(dateTimeProvider);
```

**Replace With**:
```csharp
var passwordHashResult = Guard.Against.Null(passwordHash, nameof(passwordHash));
if (passwordHashResult.IsFailure)
    return Result<Credential>.Failure(passwordHashResult.Error);

var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
if (dateTimeProviderResult.IsFailure)
    return Result<Credential>.Failure(dateTimeProviderResult.Error);
```

**Find in `UpdatePassword`**:
```csharp
ArgumentNullException.ThrowIfNull(newPasswordHash);
ArgumentNullException.ThrowIfNull(dateTimeProvider);
```

**Replace With**:
```csharp
var passwordHashResult = Guard.Against.Null(newPasswordHash, nameof(newPasswordHash));
if (passwordHashResult.IsFailure)
    return passwordHashResult;

var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
if (dateTimeProviderResult.IsFailure)
    return dateTimeProviderResult;
```

---

#### Step 1.6: Update RefreshToken Entity (5 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/RefreshToken.cs`

**Action**: Replace `ArgumentNullException.ThrowIfNull` in `Create` and `Revoke` methods.

**Pattern**: Same as above - replace with `Guard.Against.Null`.

---

#### Step 1.7: Update UserRole Entity (5 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/UserRole.cs`

**Action**: If `Create` method exists, replace `ArgumentNullException.ThrowIfNull`.

---

#### Step 1.8: Update RolePermission Entity (5 minutes)

**File**: `server/src/Product/Identity/Identity.Domain/Entities/RolePermission.cs`

**Action**: If `Create` method exists, replace `ArgumentNullException.ThrowIfNull`.

---

### Issue 2: Missing Guard Methods (30 minutes) ✅ Done

**Problem**: Guard class lacks commonly needed validation methods.

---

#### Step 2.1: Add `Guard.Against.InvalidEmail` (10 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/Guard.cs`

**Action**: Add email validation method.

```csharp
/// <summary>
/// Guards against invalid email addresses.
/// </summary>
/// <param name="email">The email address to validate</param>
/// <param name="paramName">The parameter name for error messages</param>
/// <returns>Success if email is valid; Failure with validation error if invalid</returns>
public static Result InvalidEmail(string email, string paramName = "email")
{
    if (string.IsNullOrWhiteSpace(email))
    {
        return Result.Failure(Error.Validation(
            $"{paramName}.NullOrWhiteSpace",
            $"{paramName} cannot be null or whitespace"));
    }

    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        if (addr.Address != email)
        {
            return Result.Failure(Error.Validation(
                $"{paramName}.Invalid",
                $"{paramName} is not in a valid format"));
        }
    }
    catch
    {
        return Result.Failure(Error.Validation(
            $"{paramName}.Invalid",
            $"{paramName} is not in a valid format"));
    }

    return Result.Success();
}
```

**Note**: This method is for future use. Not required for current entities.

---

#### Step 2.2: Add `Guard.Against.OutOfRange` (5 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/Guard.cs`

**Action**: Add range validation method.

```csharp
/// <summary>
/// Guards against values outside a specified range.
/// </summary>
/// <param name="value">The value to check</param>
/// <param name="min">Minimum allowed value (inclusive)</param>
/// <param name="max">Maximum allowed value (inclusive)</param>
/// <param name="paramName">The parameter name for error messages</param>
/// <returns>Success if value is in range; Failure with validation error if out of range</returns>
public static Result OutOfRange(int value, int min, int max, string paramName)
{
    if (value < min || value > max)
    {
        return Result.Failure(Error.Validation(
            $"{paramName}.OutOfRange",
            $"{paramName} must be between {min} and {max}, but was {value}"));
    }

    return Result.Success();
}
```

---

#### Step 2.3: Add `Guard.Against.InvalidLength` (5 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/Guard.cs`

**Action**: Add string length validation method.

```csharp
/// <summary>
/// Guards against strings with invalid length.
/// </summary>
/// <param name="value">The string to check</param>
/// <param name="minLength">Minimum allowed length</param>
/// <param name="maxLength">Maximum allowed length</param>
/// <param name="paramName">The parameter name for error messages</param>
/// <returns>Success if length is valid; Failure with validation error if invalid</returns>
public static Result InvalidLength(string value, int minLength, int maxLength, string paramName)
{
    if (value.Length < minLength || value.Length > maxLength)
    {
        return Result.Failure(Error.Validation(
            $"{paramName}.InvalidLength",
            $"{paramName} length must be between {minLength} and {maxLength}, but was {value.Length}"));
    }

    return Result.Success();
}
```

---

#### Step 2.4: Add `Guard.Against.InvalidGuid` (5 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/Guard.cs`

**Action**: Add GUID validation method.

```csharp
/// <summary>
/// Guards against empty GUIDs.
/// </summary>
/// <param name="value">The GUID to check</param>
/// <param name="paramName">The parameter name for error messages</param>
/// <returns>Success if GUID is not empty; Failure with validation error if empty</returns>
public static Result InvalidGuid(Guid value, string paramName)
{
    if (value == Guid.Empty)
    {
        return Result.Failure(Error.Validation(
            $"{paramName}.Empty",
            $"{paramName} cannot be an empty GUID"));
    }

    return Result.Success();
}
```

---

### Issue 3: Command Handlers Not Updated (2 hours)

**Problem**: Command handlers expect factory methods to return entities directly, not `Result<T>`.

---

#### Step 3.1: Update CreateUserCommandHandler (20 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Users/CreateUserCommandHandler.cs`

**Current Code** (likely):
```csharp
public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    // Create email value object
    var email = Email.Create(request.Email).Value; // ❌ Assumes success
    
    // Create user
    var user = User.Create(...); // ❌ Returns Result<User>, not User
    
    await _userRepository.AddAsync(user, cancellationToken);
    return Result<Guid>.Success(user.Id);
}
```

**Replace With**:
```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Create Email value object
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Error);

        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            return Result<Guid>.Failure(Error.Conflict(
                "User.EmailAlreadyExists",
                $"User with email {request.Email} already exists"));
        }

        // Create User entity
        var userResult = User.Create(
            request.DefaultTenantId,
            emailResult.Value,
            request.DisplayName,
            _dateTimeProvider);

        if (userResult.IsFailure)
            return Result<Guid>.Failure(userResult.Error);

        var user = userResult.Value;

        // Persist
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
```

**Key Changes**:
1. Inject `IDateTimeProvider`
2. Handle `Email.Create()` result
3. Handle `User.Create()` result
4. Extract `.Value` only after checking `.IsFailure`
5. Add duplicate email check

---

#### Step 3.2: Update UpdateUserCommandHandler (15 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Users/UpdateUserCommandHandler.cs`

**Replace With**:
```csharp
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(
                "User.NotFound",
                $"User with ID {request.UserId} not found"));
        }

        // Update user
        var updateResult = user.Update(request.DisplayName);
        if (updateResult.IsFailure)
            return updateResult;

        // Persist
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

**Key Changes**:
1. Handle `user.Update()` result
2. Check for null user
3. Return proper error results

---

#### Step 3.3: Update DeactivateUserCommandHandler (15 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Users/DeactivateUserCommandHandler.cs`

**Replace With**:
```csharp
public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeactivateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(
                "User.NotFound",
                $"User with ID {request.UserId} not found"));
        }

        // Deactivate user
        var deactivateResult = user.Deactivate(_dateTimeProvider);
        if (deactivateResult.IsFailure)
            return deactivateResult;

        // Persist
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

**Key Changes**:
1. Inject `IDateTimeProvider`
2. Handle `user.Deactivate()` result

---

#### Step 3.4: Update CreateRoleCommandHandler (15 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Roles/CreateRoleCommandHandler.cs`

**Replace With**:
```csharp
public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRoleCommandHandler(
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if role already exists
        var existingRole = await _roleRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existingRole is not null)
        {
            return Result<Guid>.Failure(Error.Conflict(
                "Role.CodeAlreadyExists",
                $"Role with code {request.Code} already exists"));
        }

        // Create Role entity
        var roleResult = Role.Create(
            request.Code,
            request.Name,
            request.Description,
            _dateTimeProvider);

        if (roleResult.IsFailure)
            return Result<Guid>.Failure(roleResult.Error);

        var role = roleResult.Value;

        // Persist
        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.Id);
    }
}
```

---

#### Step 3.5: Update AssignPermissionToRoleCommandHandler (15 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Roles/AssignPermissionToRoleCommandHandler.cs`

**Replace With**:
```csharp
public class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand, Result>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignPermissionToRoleCommandHandler(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        // Get role
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound(
                "Role.NotFound",
                $"Role with ID {request.RoleId} not found"));
        }

        // Verify permission exists
        var permission = await _permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
        {
            return Result.Failure(Error.NotFound(
                "Permission.NotFound",
                $"Permission with ID {request.PermissionId} not found"));
        }

        // Add permission to role
        var addResult = role.AddPermission(request.PermissionId, _dateTimeProvider);
        if (addResult.IsFailure)
            return addResult;

        // Persist
        await _roleRepository.UpdateAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

---

#### Step 3.6: Update CreatePermissionCommandHandler (15 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Permissions/CreatePermissionCommandHandler.cs`

**Replace With**:
```csharp
public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Result<Guid>>
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePermissionCommandHandler(
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        // Check if permission already exists
        var existingPermission = await _permissionRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existingPermission is not null)
        {
            return Result<Guid>.Failure(Error.Conflict(
                "Permission.CodeAlreadyExists",
                $"Permission with code {request.Code} already exists"));
        }

        // Create Permission entity
        var permissionResult = Permission.Create(
            request.Code,
            request.Name,
            request.Description,
            _dateTimeProvider);

        if (permissionResult.IsFailure)
            return Result<Guid>.Failure(permissionResult.Error);

        var permission = permissionResult.Value;

        // Persist
        await _permissionRepository.AddAsync(permission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(permission.Id);
    }
}
```

---

#### Step 3.7: Update CreateCredentialCommandHandler (15 minutes)

**File**: `server/src/Product/Identity/Identity.Application/Commands/Credentials/CreateCredentialCommandHandler.cs`

**Replace With**:
```csharp
public class CreateCredentialCommandHandler : IRequestHandler<CreateCredentialCommand, Result<Guid>>
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCredentialCommandHandler(
        ICredentialRepository credentialRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _credentialRepository = credentialRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCredentialCommand request, CancellationToken cancellationToken)
    {
        // Create PasswordHash value object
        var passwordHashResult = PasswordHash.Create(request.PasswordHash);
        if (passwordHashResult.IsFailure)
            return Result<Guid>.Failure(passwordHashResult.Error);

        // Create Credential entity
        var credentialResult = Credential.Create(
            request.UserId,
            passwordHashResult.Value,
            _dateTimeProvider);

        if (credentialResult.IsFailure)
            return Result<Guid>.Failure(credentialResult.Error);

        var credential = credentialResult.Value;

        // Persist
        await _credentialRepository.AddAsync(credential, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(credential.Id);
    }
}
```

---

### Issue 4: ValidationBehavior Result<T> Handling (30 minutes) ✅ Already correct

**Problem**: `ValidationBehavior` may not correctly create `Result<T>.Failure(error)` for all response types.

---

#### Step 4.1: Update ValidationBehavior (30 minutes)

**File**: `server/src/BuildingBlocks/Application/Behaviors/ValidationBehavior.cs`

**Find**:
```csharp
var error = Error.Validation(
    code: $"{typeof(TRequest).Name}.Validation",
    message: message);

return ResultResponseFactory.CreateFailure<TResponse>(error);
```

**Replace With**:
```csharp
var error = Error.Validation(
    code: $"{typeof(TRequest).Name}.Validation",
    message: message);

// Use reflection to create Result<T>.Failure(error) or Result.Failure(error)
var responseType = typeof(TResponse);

// Check if TResponse is Result<T>
if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
{
    var failureMethod = responseType.GetMethod(
        "Failure",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
        null,
        new[] { typeof(Error) },
        null);

    if (failureMethod is not null)
    {
        return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
    }
}

// Check if TResponse is Result
if (responseType == typeof(Result))
{
    return (TResponse)(object)Result.Failure(error);
}

// Fallback: throw exception
throw new ValidationException(failures);
```

**Complete Method**:
```csharp
public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    if (!_validators.Any())
    {
        return await next();
    }

    var context = new ValidationContext<TRequest>(request);

    var validationResults = await Task.WhenAll(
        _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

    var failures = validationResults
        .SelectMany(r => r.Errors)
        .Where(f => f is not null)
        .ToList();

    if (failures.Count == 0)
    {
        return await next();
    }

    var message = string.Join(
        "; ",
        failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));

    var error = Error.Validation(
        code: $"{typeof(TRequest).Name}.Validation",
        message: message);

    // Use reflection to create Result<T>.Failure(error) or Result.Failure(error)
    var responseType = typeof(TResponse);

    // Check if TResponse is Result<T>
    if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
    {
        var failureMethod = responseType.GetMethod(
            "Failure",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null,
            new[] { typeof(Error) },
            null);

        if (failureMethod is not null)
        {
            return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
        }
    }

    // Check if TResponse is Result
    if (responseType == typeof(Result))
    {
        return (TResponse)(object)Result.Failure(error);
    }

    // Fallback: throw exception
    throw new ValidationException(failures);
}
```

---

### Issue 5: EF Core Value Object Configuration (1 hour)

**Problem**: EF Core needs configuration for `Email` and `PasswordHash` value objects.

---

#### Step 5.1: Update UserConfiguration (20 minutes)

**File**: `server/src/Product/Identity/Identity.Infrastructure/Persistence/Configurations/UserConfiguration.cs`

**Find**:
```csharp
builder.Property(u => u.Email)
    .HasMaxLength(256)
    .IsRequired();
```

**Replace With**:
```csharp
// Value object configuration for Email
builder.OwnsOne(u => u.Email, email =>
{
    email.Property(e => e.Value)
        .HasColumnName("Email")
        .HasMaxLength(256)
        .IsRequired();

    email.HasIndex(e => e.Value)
        .IsUnique()
        .HasDatabaseName("IX_Users_Email");
});
```

**Complete Configuration**:
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        // Value object configuration for Email
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            email.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
        });

        builder.Property(u => u.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.DefaultTenantId)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.IsEmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.EmailConfirmedAt)
            .IsRequired(false);

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(u => u.DefaultTenantId)
            .HasDatabaseName("IX_Users_DefaultTenantId");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");
    }
}
```

---

#### Step 5.2: Update CredentialConfiguration (20 minutes)

**File**: `server/src/Product/Identity/Identity.Infrastructure/Persistence/Configurations/CredentialConfiguration.cs`

**Find**:
```csharp
builder.Property(c => c.PasswordHash)
    .HasMaxLength(500)
    .IsRequired();
```

**Replace With**:
```csharp
// Value object configuration for PasswordHash
builder.OwnsOne(c => c.PasswordHash, passwordHash =>
{
    passwordHash.Property(p => p.Value)
        .HasColumnName("PasswordHash")
        .HasMaxLength(500)
        .IsRequired();
});
```

**Complete Configuration**:
```csharp
public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("Credentials");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        // Value object configuration for PasswordHash
        builder.OwnsOne(c => c.PasswordHash, passwordHash =>
        {
            passwordHash.Property(p => p.Value)
                .HasColumnName("PasswordHash")
                .HasMaxLength(500)
                .IsRequired();
        });

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Credentials_UserId");
    }
}
```

---

#### Step 5.3: Verify Other Configurations (20 minutes)

**Files to Check**:
- `RoleConfiguration.cs`
- `PermissionConfiguration.cs`
- `RefreshTokenConfiguration.cs`
- `UserRoleConfiguration.cs`
- `RolePermissionConfiguration.cs`

**Action**: Ensure all configurations:
1. Use `ToTable()` with proper table name
2. Use `HasKey()` for primary key
3. Use `ValueGeneratedNever()` for GUID primary keys
4. Use `IsRequired()` for non-nullable properties
5. Use `HasMaxLength()` for string properties
6. Use `HasIndex()` for foreign keys and frequently queried columns

---

### Issue 6: Database Migration (30 minutes)

**Problem**: Database schema needs migration for value object changes.

---

#### Step 6.1: Create Migration (15 minutes)

**Action**: Run EF Core migration command.

**Command**:
```bash
cd server/src/Product/Identity/Identity.Infrastructure
dotnet ef migrations add RefactorToValueObjects --startup-project ../../Hosts/MonolithHost --context IdentityDbContext
```

**Expected Output**:
- New migration file in `Migrations/` folder
- Migration should show:
  - No schema changes (Email and PasswordHash already stored as strings)
  - Possible index changes if unique constraints added

---

#### Step 6.2: Review Migration (10 minutes)

**File**: `server/src/Product/Identity/Identity.Infrastructure/Migrations/{timestamp}_RefactorToValueObjects.cs`

**Action**: Review generated migration.

**Expected Changes**:
- Unique index on `Users.Email` (if not already present)
- Unique index on `Credentials.UserId` (if not already present)
- No column renames (Email.Value maps to Email column)

**If Migration Shows Column Renames**:
- This means EF Core thinks Email is a new column
- Fix: Add `.HasColumnName("Email")` in `OwnsOne` configuration
- Delete migration and recreate

---

#### Step 6.3: Apply Migration (5 minutes)

**Action**: Apply migration to database.

**Command**:
```bash
cd server/src/Product/Identity/Identity.Infrastructure
dotnet ef database update --startup-project ../../Hosts/MonolithHost --context IdentityDbContext
```

**Validation**:
- Check database schema
- Verify Email column still exists (not renamed)
- Verify unique indexes created

---

### Issue 7: Domain Events Missing Timestamp (30 minutes)

**Problem**: Some domain events don't include `OccurredAt` timestamp.

---

#### Step 7.1: Create DomainEvent Base Record (10 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/DomainEvent.cs`

**Action**: Create base record for domain events.

```csharp
namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Base record for domain events with automatic timestamp.
/// All domain events should inherit from this record.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Creates a new domain event with current UTC timestamp.
    /// </summary>
    protected DomainEvent()
    {
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new domain event with specified timestamp.
    /// Use this constructor when you need deterministic timestamps (e.g., from IDateTimeProvider).
    /// </summary>
    /// <param name="occurredAt">When the event occurred (UTC)</param>
    protected DomainEvent(DateTime occurredAt)
    {
        if (occurredAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("OccurredAt must be UTC", nameof(occurredAt));

        OccurredAt = occurredAt;
    }
}
```

---

#### Step 7.2: Update Domain Events to Inherit from DomainEvent (20 minutes)

**Files to Update**:
- `UserCreatedEvent.cs`
- `UserEmailConfirmedEvent.cs`
- `RoleCreatedEvent.cs`
- `PermissionCreatedEvent.cs`
- `PermissionGrantedToRoleEvent.cs`
- `PermissionRevokedFromRoleEvent.cs`
- `RefreshTokenCreatedEvent.cs`
- `RefreshTokenRevokedEvent.cs`

**Current Pattern**:
```csharp
public record UserCreatedEvent(
    Guid UserId,
    Guid DefaultTenantId,
    string Email,
    DateTime OccurredAt) : IDomainEvent;
```

**Replace With**:
```csharp
public record UserCreatedEvent(
    Guid UserId,
    Guid DefaultTenantId,
    string Email) : DomainEvent
{
    public UserCreatedEvent(
        Guid userId,
        Guid defaultTenantId,
        string email,
        DateTime occurredAt) : base(occurredAt)
    {
        UserId = userId;
        DefaultTenantId = defaultTenantId;
        Email = email;
    }
}
```

**Or Simpler (if timestamp not needed in constructor)**:
```csharp
public record UserCreatedEvent(
    Guid UserId,
    Guid DefaultTenantId,
    string Email) : DomainEvent;
```

**Update Entity Event Raising**:

**Find in `User.cs`**:
```csharp
user.RaiseDomainEvent(new UserCreatedEvent(
    user.Id,
    user.DefaultTenantId,
    user.Email.Value,
    now));
```

**Replace With**:
```csharp
user.RaiseDomainEvent(new UserCreatedEvent(
    user.Id,
    user.DefaultTenantId,
    user.Email.Value)
{
    OccurredAt = now
});
```

**Or Keep Constructor**:
```csharp
user.RaiseDomainEvent(new UserCreatedEvent(
    user.Id,
    user.DefaultTenantId,
    user.Email.Value,
    now));
```

---

### Issue 8: Result Extension Methods (30 minutes) ✅ Done

**Problem**: Command handlers have repetitive `Result<T>` handling code.

---

#### Step 8.1: Create ResultExtensions (30 minutes)

**File**: `server/src/BuildingBlocks/Kernel/Domain/ResultExtensions.cs`

**Action**: Create extension methods for cleaner Result handling.

```csharp
namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Extension methods for Result and Result{T} to enable functional composition.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a Result{T} to Result{TNew} using a transformation function.
    /// Only executes the mapper if the result is successful.
    /// </summary>
    /// <typeparam name="T">Source result type</typeparam>
    /// <typeparam name="TNew">Target result type</typeparam>
    /// <param name="result">The source result</param>
    /// <param name="mapper">Function to transform T to TNew</param>
    /// <returns>Result{TNew} with mapped value or original error</returns>
    public static Result<TNew> Map<T, TNew>(this Result<T> result, Func<T, TNew> mapper)
    {
        return result.IsSuccess
            ? Result<TNew>.Success(mapper(result.Value))
            : Result<TNew>.Failure(result.Error);
    }

    /// <summary>
    /// Binds a Result{T} to Result{TNew} using a function that returns Result{TNew}.
    /// Only executes the binder if the result is successful.
    /// Useful for chaining operations that can fail.
    /// </summary>
    /// <typeparam name="T">Source result type</typeparam>
    /// <typeparam name="TNew">Target result type</typeparam>
    /// <param name="result">The source result</param>
    /// <param name="binder">Function to transform T to Result{TNew}</param>
    /// <returns>Result{TNew} from binder or original error</returns>
    public static Result<TNew> Bind<T, TNew>(this Result<T> result, Func<T, Result<TNew>> binder)
    {
        return result.IsSuccess
            ? binder(result.Value)
            : Result<TNew>.Failure(result.Error);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// Returns the original result.
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="result">The result</param>
    /// <param name="action">Action to execute on success</param>
    /// <returns>The original result</returns>
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Combines multiple Result objects. Returns failure if any result failed.
    /// Returns the first failure encountered.
    /// </summary>
    /// <param name="results">Results to combine</param>
    /// <returns>Success if all results succeeded; first failure otherwise</returns>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    /// <summary>
    /// Combines multiple Result objects. Returns failure if any result failed.
    /// Collects all errors if multiple failures occur.
    /// </summary>
    /// <param name="results">Results to combine</param>
    /// <returns>Success if all results succeeded; combined failure otherwise</returns>
    public static Result CombineAll(params Result[] results)
    {
        var failures = results.Where(r => r.IsFailure).ToList();

        if (failures.Count == 0)
            return Result.Success();

        if (failures.Count == 1)
            return failures[0];

        var combinedMessage = string.Join("; ", failures.Select(f => f.Error.Message));
        var combinedCode = string.Join(", ", failures.Select(f => f.Error.Code));

        return Result.Failure(Error.Validation(combinedCode, combinedMessage));
    }

    /// <summary>
    /// Converts Result{T} to Result by discarding the value.
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="result">The result to convert</param>
    /// <returns>Result without value</returns>
    public static Result ToResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error);
    }
}
```

**Usage Example**:
```csharp
// Before (verbose)
var emailResult = Email.Create(request.Email);
if (emailResult.IsFailure)
    return Result<Guid>.Failure(emailResult.Error);

var userResult = User.Create(
    request.DefaultTenantId,
    emailResult.Value,
    request.DisplayName,
    _dateTimeProvider);

if (userResult.IsFailure)
    return Result<Guid>.Failure(userResult.Error);

return Result<Guid>.Success(userResult.Value.Id);

// After (functional)
return Email.Create(request.Email)
    .Bind(email => User.Create(
        request.DefaultTenantId,
        email,
        request.DisplayName,
        _dateTimeProvider))
    .Map(user => user.Id);
```

---

### Issue 9: Repository Specification Support (30 minutes) ✅ Already supported

**Problem**: Repository implementations may not properly support Ardalis.Specification.

---

#### Step 9.1: Verify Base Repository (15 minutes)

**File**: `server/src/BuildingBlocks/Infrastructure/Persistence/Repository.cs`

**Action**: Verify base repository implements specification methods.

**Expected Implementation**:
```csharp
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;

public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // Specification methods
    public async Task<TEntity?> GetBySpecAsync(
        ISpecification<TEntity> spec,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .WithSpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TEntity>> ListAsync(
        ISpecification<TEntity> spec,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .WithSpecification(spec)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        ISpecification<TEntity> spec,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .WithSpecification(spec)
            .CountAsync(cancellationToken);
    }

    // Standard CRUD methods
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}
```

**If Missing**: Add specification methods.

---

#### Step 9.2: Verify Identity Repositories (15 minutes)

**Files to Check**:
- `UserRepository.cs`
- `RoleRepository.cs`
- `PermissionRepository.cs`
- `RefreshTokenRepository.cs`

**Action**: Verify each repository inherits from `Repository<TEntity, TKey>`.

**Expected Pattern**:
```csharp
public class UserRepository : Repository<User, Guid>, IUserRepository
{
    public UserRepository(IdentityDbContext context) : base(context)
    {
    }

    // Custom query methods
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.Value == email, cancellationToken);
    }

    // Specification methods inherited from Repository<User, Guid>:
    // - GetBySpecAsync(ISpecification<User> spec)
    // - ListAsync(ISpecification<User> spec)
    // - CountAsync(ISpecification<User> spec)
}
```

**If Not Inheriting**: Update to inherit from base repository.

---

## Summary Checklist

### Critical (Must Complete)
- [x] Add `Guard.Against.Null` method (and Result-returning NullOrWhiteSpace, EmptyGuid)
- [x] Replace all `ArgumentNullException.ThrowIfNull` in 7 entities (User, Role, Permission, Credential, RefreshToken, UserRole, RolePermission)
- [x] Update command handlers to handle `Result<T>` (CreateUserCommandHandler, UpdateUserCommandHandler already do; Deactivate/CreateRole/etc. when added)
- [x] Fix `ValidationBehavior` to create `Result<T>.Failure` (already implemented via ResultResponseFactory)

### High Priority (Should Complete)
- [x] Add missing Guard methods (InvalidEmail, OutOfRange, InvalidLength, EmptyGuid – in BuildingBlocks.Kernel/Domain/Guard.cs)
- [ ] Add EF Core value object configurations (User, Credential) – when Identity.Infrastructure has DbContext and configurations
- [ ] Create and apply database migration – when configurations exist
- [x] Verify repository implementations support specifications (IRepository has FirstOrDefaultAsync/ListAsync/CountAsync with ISpecification)

### Medium Priority (Nice to Have)
- [x] Create `DomainEvent` base record (already exists in BuildingBlocks.Kernel/Domain/DomainEvent.cs with OccurredOn)
- [ ] Update domain events to inherit from `DomainEvent` (Identity events use IDomainEvent; optional)
- [x] Add `ResultExtensions` for functional composition (BuildingBlocks.Kernel/Results/ResultExtensions.cs)

---

## Estimated Timeline

| Task | Effort |
|------|--------|
| **Issue 1: Guard Clauses** | 1 hour |
| **Issue 2: Guard Methods** | 30 minutes |
| **Issue 3: Command Handlers** | 2 hours |
| **Issue 4: ValidationBehavior** | 30 minutes |
| **Issue 5: EF Core Configurations** | 1 hour |
| **Issue 6: Database Migration** | 30 minutes |
| **Issue 7: Domain Events** | 30 minutes |
| **Issue 8: Result Extensions** | 30 minutes |
| **Issue 9: Repository Verification** | 30 minutes |
| **Total** | **~7.5 hours** |

---

## Validation Steps

After completing all fixes:

1. **Build Solution**
   ```bash
   dotnet build
   ```
   - Should compile without errors

2. **Run Migrations**
   ```bash
   dotnet ef database update
   ```
   - Should apply successfully

3. **Run Application**
   ```bash
   dotnet run --project server/src/Hosts/MonolithHost
   ```
   - Should start without errors

4. **Test API Endpoints**
   - POST /api/users (create user)
   - GET /api/users/{id} (get user)
   - PUT /api/users/{id} (update user)
   - All should return proper Result<T> responses

---

## Next Steps

After completing these fixes:

1. **Write Unit Tests** (separate plan)
   - Entity factory method tests
   - Entity state method tests
   - Command handler tests
   - Guard method tests

2. **Write Integration Tests** (separate plan)
   - API endpoint tests
   - Database interaction tests
   - Domain event handling tests

3. **Phase 2: Identity.Application** (separate plan)
   - Add missing validators
   - Add missing specifications
   - Add missing query handlers

---

*End of Fixes Plan*