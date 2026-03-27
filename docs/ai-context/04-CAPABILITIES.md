# Datarizen AI Context - Capabilities

## Overview

Capabilities are cross-cutting concerns and non-business-logic features used across modules. Unlike BuildingBlocks (which provide technical primitives), Capabilities provide complete feature implementations.

**Key Differences**:
- **BuildingBlocks**: Technical primitives (Result, Entity, Repository interfaces) - NO vendor dependencies
- **Capabilities**: Complete implementations (Multi-tenancy, Authentication, File Storage) - CAN have vendor dependencies

## Project Structure

```
/server/src/Capabilities
  /MultiTenancy
    Datarizen.Capabilities.MultiTenancy.csproj
    /Middleware
      TenantResolutionMiddleware.cs
    /Services
      ITenantService.cs
      TenantService.cs
    /Context
      ITenantContext.cs
      TenantContext.cs
  
  /Authentication
    Datarizen.Capabilities.Authentication.csproj
    /Jwt
      JwtTokenGenerator.cs
      JwtTokenValidator.cs
    /Services
      IAuthenticationService.cs
      AuthenticationService.cs
  
  /Authorization
    Datarizen.Capabilities.Authorization.csproj
    /Policies
      TenantPolicyHandler.cs
      PermissionPolicyHandler.cs
    /Requirements
      TenantRequirement.cs
      PermissionRequirement.cs
  
  /Auditing
    Datarizen.Capabilities.Auditing.csproj
    /Interceptors
      AuditInterceptor.cs
    /Services
      IAuditService.cs
      AuditService.cs
  
  /FileStorage
    Datarizen.Capabilities.FileStorage.csproj
    /Abstractions
      IFileStorageService.cs
    /Providers
      LocalFileStorageService.cs
      AzureBlobStorageService.cs
      S3StorageService.cs
  
  /Notifications
    Datarizen.Capabilities.Notifications.csproj
    /Email
      IEmailService.cs
      SmtpEmailService.cs
    /Push
      IPushNotificationService.cs
  
  /Chat
    Datarizen.Capabilities.Chat.csproj
    /SignalR
      ChatHub.cs
    /Services
      IChatService.cs
  
  /FeatureFlags
    Datarizen.Capabilities.FeatureFlags.csproj
    /Services
      IFeatureFlagService.cs
      FeatureFlagService.cs
```

---

## Multi-Tenancy

### Tenant Context

```csharp
// ITenantContext.cs
public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantIdentifier { get; }
    void SetTenant(Guid tenantId, string identifier);
    void Clear();
}

// TenantContext.cs
public sealed class TenantContext : ITenantContext
{
    private readonly AsyncLocal<TenantInfo?> _currentTenant = new();
    
    public Guid? TenantId => _currentTenant.Value?.TenantId;
    public string? TenantIdentifier => _currentTenant.Value?.Identifier;
    
    public void SetTenant(Guid tenantId, string identifier)
    {
        _currentTenant.Value = new TenantInfo(tenantId, identifier);
    }
    
    public void Clear()
    {
        _currentTenant.Value = null;
    }
    
    private sealed record TenantInfo(Guid TenantId, string Identifier);
}
```

**Key Points**:
- ✅ Uses `AsyncLocal<T>` for async-safe tenant context
- ✅ Thread-safe and async-safe
- ✅ Scoped to async execution context

---

### Tenant Resolution Middleware

```csharp
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantService tenantService)
    {
        // Try to resolve tenant from:
        // 1. Subdomain (tenant1.datarizen.com)
        // 2. Header (X-Tenant-Id)
        // 3. JWT claim (tenant_id)
        
        var tenantIdentifier = ResolveTenantIdentifier(context);
        
        if (!string.IsNullOrEmpty(tenantIdentifier))
        {
            var tenant = await tenantService.GetByIdentifierAsync(tenantIdentifier);
            if (tenant is not null)
            {
                tenantContext.SetTenant(tenant.Id, tenant.Identifier);
                _logger.LogInformation("Tenant resolved: {TenantId}", tenant.Id);
            }
        }
        
        await _next(context);
        tenantContext.Clear();
    }
    
    private string? ResolveTenantIdentifier(HttpContext context)
    {
        // Priority 1: From subdomain
        var host = context.Request.Host.Host;
        if (host.Contains('.'))
        {
            var subdomain = host.Split('.')[0];
            if (subdomain != "www" && subdomain != "api" && subdomain != "localhost")
                return subdomain;
        }
        
        // Priority 2: From header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue))
            return headerValue.ToString();
        
        // Priority 3: From JWT claim
        var tenantClaim = context.User.FindFirst("tenant_id");
        return tenantClaim?.Value;
    }
}
```

---

### Tenant Service

```csharp
public interface ITenantService
{
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<Result<Tenant>> CreateAsync(string identifier, string name, CancellationToken cancellationToken = default);
}

public class TenantService : ITenantService
{
    private readonly IRepository<Tenant> _repository;
    private readonly ICacheService _cache;
    
    public async Task<Result<Tenant>> CreateAsync(
        string identifier,
        string name,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdentifierAsync(identifier, cancellationToken);
        if (existing is not null)
        {
            return Result<Tenant>.Failure(
                Error.Conflict("Tenant.IdentifierAlreadyExists", "Tenant with this identifier already exists"));
        }
        
        var tenantResult = Tenant.Create(identifier, name);
        if (tenantResult.IsFailure)
            return tenantResult;
        
        await _repository.AddAsync(tenantResult.Value, cancellationToken);
        
        return tenantResult;
    }
}
```

---

### Global Query Filter

```csharp
// In Module DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply tenant filter to all tenant-scoped entities
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
        {
            var method = typeof(TenantQueryFilterExtensions)
                .GetMethod(nameof(TenantQueryFilterExtensions.ApplyTenantFilter))!
                .MakeGenericMethod(entityType.ClrType);
            
            method.Invoke(null, new object[] { modelBuilder, _tenantContext });
        }
    }
}

// Extension method
public static class TenantQueryFilterExtensions
{
    public static void ApplyTenantFilter<TEntity>(
        ModelBuilder modelBuilder,
        ITenantContext tenantContext)
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
    }
}

// Interface for tenant-scoped entities
public interface ITenantScoped
{
    Guid TenantId { get; }
}
```

---

### Tenant Entity

```csharp
public sealed class Tenant : Entity<Guid>
{
    public string Identifier { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private Tenant() { } // EF Core
    
    public static Result<Tenant> Create(string identifier, string name)
    {
        var validationResult = Guard.Against.NullOrWhiteSpace(identifier, nameof(identifier))
            .Combine(() => Guard.Against.NullOrWhiteSpace(name, nameof(name)))
            .Combine(() => Guard.Against.InvalidLength(identifier, nameof(identifier), 3, 50))
            .Combine(() => Guard.Against.InvalidLength(name, nameof(name), 1, 200));
        
        if (validationResult.IsFailure)
            return Result<Tenant>.Failure(validationResult.Error);
        
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Identifier = identifier.ToLowerInvariant(),
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        return Result<Tenant>.Success(tenant);
    }
}
```

---

## Authorization

### Authorization Requirements

```csharp
public sealed class TenantRequirement : IAuthorizationRequirement
{
    public Guid TenantId { get; }
    
    public TenantRequirement(Guid tenantId)
    {
        TenantId = tenantId;
    }
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
```

---

### Authorization Handlers

```csharp
public class TenantAuthorizationHandler : AuthorizationHandler<TenantRequirement>
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantAuthorizationHandler> _logger;
    
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement)
    {
        if (_tenantContext.TenantId == requirement.TenantId)
        {
            _logger.LogDebug("Tenant access granted: {TenantId}", requirement.TenantId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Tenant access denied: Required {RequiredTenantId}, Current {CurrentTenantId}",
                requirement.TenantId,
                _tenantContext.TenantId);
        }
        
        return Task.CompletedTask;
    }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            _logger.LogWarning("User ID not found in claims");
            return;
        }
        
        var hasPermission = await _permissionService.HasPermissionAsync(
            Guid.Parse(userId),
            requirement.Permission);
        
        if (hasPermission)
        {
            _logger.LogDebug("Permission granted: {UserId} has {Permission}", userId, requirement.Permission);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Permission denied: {UserId} does not have {Permission}", userId, requirement.Permission);
        }
    }
}
```

---

### Registration

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("TenantAccess", policy =>
                policy.Requirements.Add(new TenantRequirement(Guid.Empty)));
            
            options.AddPolicy("ManageUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users.manage")));
            
            options.AddPolicy("ViewUsers", policy =>
                policy.Requirements.Add(new PermissionRequirement("users.view")));
        });
        
        services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        
        return services;
    }
}
```

---

## Auditing

### Audit Interceptor

```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        
        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditable auditable)
            {
                var now = _dateTimeProvider.UtcNow;
                
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                }
                else
                {
                    auditable.UpdatedAt = now;
                }
            }
            
            if (entry.Entity is ITenantScoped tenantScoped && entry.State == EntityState.Added)
            {
                if (_tenantContext.TenantId.HasValue)
                {
                    tenantScoped.TenantId = _tenantContext.TenantId.Value;
                }
            }
        }
        
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

---

## File Storage

### Abstraction

```csharp
public interface IFileStorageService
{
    Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
    
    Task<Result<Stream>> DownloadAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
    
    Task<Result> DeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
    
    Task<Result<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
    
    Task<Result<string>> GetPresignedUrlAsync(
        string fileUrl,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
}
```

---

### Local File Storage

```csharp
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;
    
    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _basePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }
    
    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_basePath, uniqueFileName);
            
            using var fileStreamOutput = File.Create(filePath);
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
            
            _logger.LogInformation("File uploaded locally: {FileName}", uniqueFileName);
            
            return Result<string>.Success(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            return Result<string>.Failure(Error.Failure("FileStorage.UploadFailed", ex.Message));
        }
    }
}
```

---

### Azure Blob Storage

```csharp
public sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<AzureBlobStorageService> _logger;
    
    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        var connectionString = configuration["FileStorage:Azure:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Blob Storage connection string not configured");
        
        _containerName = configuration["FileStorage:Azure:ContainerName"] ?? "uploads";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }
    
    public async Task<Result<string>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            
            await blobClient.UploadAsync(
                fileStream,
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken);
            
            _logger.LogInformation("File uploaded to Azure Blob: {FileName}", uniqueFileName);
            
            return Result<string>.Success(blobClient.Uri.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Azure: {FileName}", fileName);
            return Result<string>.Failure(Error.Failure("FileStorage.UploadFailed", ex.Message));
        }
    }
    
    public async Task<Result<string>> GetPresignedUrlAsync(
        string fileUrl,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(fileUrl));
            
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return Result<string>.Failure(
                    Error.NotFound("FileStorage.FileNotFound", "File not found"));
            }
            
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiration)
            };
            
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            
            return Result<string>.Success(sasUri.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL");
            return Result<string>.Failure(Error.Failure("FileStorage.PresignedUrlFailed", ex.Message));
        }
    }
}
```

---

## Notifications

### Email Service

```csharp
public interface IEmailService
{
    Task<Result> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
    
    Task<Result> SendAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
    
    Task<Result> SendWithTemplateAsync(
        string to,
        string templateName,
        object model,
        CancellationToken cancellationToken = default);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    
    public async Task<Result> SendAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage();
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;
            
            using var smtpClient = new SmtpClient(_configuration["Email:SmtpHost"])
            {
                Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]),
                EnableSsl = true
            };
            
            await smtpClient.SendMailAsync(message, cancellationToken);
            
            _logger.LogInformation("Email sent to {To}", to);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return Result.Failure(Error.Failure("Email.SendFailed", ex.Message));
        }
    }
}
```

---

## Feature Flags

```csharp
public interface IFeatureFlagService
{
    Task<Result<bool>> IsEnabledAsync(
        string featureName,
        CancellationToken cancellationToken = default);
    
    Task<Result<bool>> IsEnabledForTenantAsync(
        string featureName,
        Guid tenantId,
        CancellationToken cancellationToken = default);
    
    Task<Result<bool>> IsEnabledForUserAsync(
        string featureName,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IRepository<FeatureFlag> _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<FeatureFlagService> _logger;
    
    public async Task<Result<bool>> IsEnabledAsync(
        string featureName,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"feature:{featureName}";
        
        var cached = await _cache.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cached.HasValue)
            return Result<bool>.Success(cached.Value);
        
        var feature = await _repository.FirstOrDefaultAsync(
            f => f.Name == featureName && f.TenantId == null && f.UserId == null,
            cancellationToken);
        
        var isEnabled = feature?.IsEnabled ?? false;
        
        await _cache.SetAsync(cacheKey, isEnabled, TimeSpan.FromMinutes(10), cancellationToken);
        
        _logger.LogDebug("Feature flag {FeatureName}: {IsEnabled}", featureName, isEnabled);
        
        return Result<bool>.Success(isEnabled);
    }
    
    public async Task<Result<bool>> IsEnabledForTenantAsync(
        string featureName,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"feature:{featureName}:tenant:{tenantId}";
        
        var cached = await _cache.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cached.HasValue)
            return Result<bool>.Success(cached.Value);
        
        // Check tenant-specific flag first
        var tenantFeature = await _repository.FirstOrDefaultAsync(
            f => f.Name == featureName && f.TenantId == tenantId,
            cancellationToken);
        
        if (tenantFeature is not null)
        {
            await _cache.SetAsync(cacheKey, tenantFeature.IsEnabled, TimeSpan.FromMinutes(10), cancellationToken);
            return Result<bool>.Success(tenantFeature.IsEnabled);
        }
        
        // Fall back to global flag
        var globalResult = await IsEnabledAsync(featureName, cancellationToken);
        if (globalResult.IsFailure)
            return globalResult;
        
        await _cache.SetAsync(cacheKey, globalResult.Value, TimeSpan.FromMinutes(10), cancellationToken);
        
        return globalResult;
    }
}

// Feature Flag Entity
public sealed class FeatureFlag : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private FeatureFlag() { } // EF Core
    
    public static Result<FeatureFlag> Create(
        string name,
        bool isEnabled,
        Guid? tenantId = null,
        Guid? userId = null,
        string? description = null)
    {
        var validationResult = Guard.Against.NullOrWhiteSpace(name, nameof(name))
            .Combine(() => Guard.Against.InvalidLength(name, nameof(name), 1, 100));
        
        if (validationResult.IsFailure)
            return Result<FeatureFlag>.Failure(validationResult.Error);
        
        var featureFlag = new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsEnabled = isEnabled,
            TenantId = tenantId,
            UserId = userId,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        
        return Result<FeatureFlag>.Success(featureFlag);
    }
    
    public Result Toggle()
    {
        IsEnabled = !IsEnabled;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
```

---

## Summary

**Capabilities provide**:
- ✅ Multi-tenancy (context, resolution, isolation, query filters)
- ✅ Authentication (JWT generation/validation)
- ✅ Authorization (policy-based, permissions, tenant-based)
- ✅ Auditing (interceptors, audit logs, change tracking)
- ✅ File storage (local, Azure Blob, S3) with Result pattern
- ✅ Notifications (email with SMTP)
- ✅ Feature flags (global, per-tenant, per-user)

**All capabilities**:
- ✅ Return `Result<T>` for operations that can fail
- ✅ Use `IDateTimeProvider` for deterministic timestamps
- ✅ Use `Guard.Against.*` methods for validation
- ✅ Support caching for performance
- ✅ Include comprehensive logging
- ✅ Follow dependency injection patterns
- ✅ Work across all deployment topologies

**All modules can**:
- Reference any Capability project
- Use capabilities via dependency injection
- Enable/disable capabilities per deployment
- Override capability implementations
- Combine multiple capabilities (e.g., multi-tenancy + auditing + authorization)

**Key Patterns**:
- ✅ `ITenantContext` for current tenant access
- ✅ `ITenantScoped` interface for tenant-scoped entities
- ✅ `IAuditable` interface for auditable entities
- ✅ Global query filters for automatic tenant isolation
- ✅ EF Core interceptors for cross-cutting concerns
- ✅ Policy-based authorization with custom requirements
- ✅ Provider pattern for pluggable implementations (file storage)
- ✅ Feature flags with hierarchical fallback (user → tenant → global)

**Technology Stack**:
- ✅ .NET 10 with C# 13
- ✅ ASP.NET Core for middleware and HTTP context
- ✅ Entity Framework Core 10.x for data access
- ✅ Azure SDK for Azure Blob Storage
- ✅ System.Net.Mail for SMTP email
- ✅ SignalR for real-time chat (if implemented)
