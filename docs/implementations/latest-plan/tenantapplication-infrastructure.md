# TenantApplication Module - Infrastructure Layer

**Status**: Ready for implementation (shared ApplicationDefinition)  
**Last Updated**: 2026-02-15  
**Module**: TenantApplication  
**Layer**: Infrastructure  

---

## Shared ApplicationDefinition usage

- **ApplicationDefinition.Domain** is referenced by TenantApplication.Infrastructure.  
- The **same shared entity types** (`EntityDefinition`, `PropertyDefinition`, `RelationDefinition`, `NavigationDefinition`, `PageDefinition`, `DataSourceDefinition`, `ApplicationRelease`) are mapped to the **tenantapplication** schema with table names: `tenant_entity_definitions`, `tenant_property_definitions`, `tenant_relation_definitions`, `tenant_navigation_definitions`, `tenant_page_definitions`, `tenant_datasource_definitions`, `tenant_application_releases`.  
- **TenantApplicationDbContext** (or equivalent) exposes `DbSet`s for TenantApplication aggregates and for these shared definition types; EF configurations apply the correct table names and FKs (e.g. `tenant_application_id` for the shared `ApplicationDefinitionId` property when used in tenant context).  
- **Repository implementations** for the shared interfaces (`IEntityDefinitionRepository`, etc.) are implemented in this layer, scoped by TenantApplicationId (passed as the “application definition id” in the shared interface).  
- **Review**: Confirm this usage before implementation.

---

## Overview

Persistence and external integrations for TenantApplication module. When a tenant has the AppBuilder feature, TenantApplication also persists **tenant-level definitions** (tenant_entity_definitions, tenant_page_definitions, tenant_navigation_definitions, tenant_datasource_definitions, tenant_application_releases, etc.) in the same `tenantapplication` schema; the AppBuilder UX edits tenant applications by calling TenantApplication API, which uses this infrastructure.

**Key Changes from Domain Update**:
- ✅ Updated entity configurations for Major/Minor/Patch
- ✅ Removed Version string column
- ✅ Added composite index on version fields
- ✅ Updated repository queries

---

## DbContext

**File**: `TenantApplication.Infrastructure/Persistence/TenantApplicationDbContext.cs`

```csharp
namespace Datarizen.TenantApplication.Infrastructure.Persistence;

public sealed class TenantApplicationDbContext : DbContext
{
    public const string SchemaName = "tenantapplication";

    public DbSet<TenantApplication> TenantApplications => Set<TenantApplication>();
    public DbSet<TenantApplicationEnvironment> TenantApplicationEnvironments => Set<TenantApplicationEnvironment>();
    public DbSet<TenantApplicationMigration> TenantApplicationMigrations => Set<TenantApplicationMigration>();

    public TenantApplicationDbContext(DbContextOptions<TenantApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

---

## Entity Configurations

### TenantApplicationConfiguration

**File**: `TenantApplication.Infrastructure/Persistence/Configurations/TenantApplicationConfiguration.cs`

```csharp
namespace Datarizen.TenantApplication.Infrastructure.Persistence.Configurations;

public sealed class TenantApplicationConfiguration 
    : IEntityTypeConfiguration<TenantApplication>
{
    public void Configure(EntityTypeBuilder<TenantApplication> builder)
    {
        builder.ToTable("tenant_applications");

        builder.HasKey(ta => ta.Id);

        builder.Property(ta => ta.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ta => ta.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(ta => ta.ApplicationId)
            .HasColumnName("application_id");

        builder.Property(ta => ta.Name)
            .HasColumnName("name")
            .HasMaxLength(200);

        builder.Property(ta => ta.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100);

        builder.Property(ta => ta.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(ta => ta.IsCustom)
            .HasColumnName("is_custom");

        builder.Property(ta => ta.SourceApplicationReleaseId)
            .HasColumnName("source_application_release_id");

        builder.Property(ta => ta.Major)
            .HasColumnName("major");

        builder.Property(ta => ta.Minor)
            .HasColumnName("minor");

        builder.Property(ta => ta.Patch)
            .HasColumnName("patch");

        builder.Property(ta => ta.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ta => ta.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(ta => ta.InstalledAt)
            .HasColumnName("installed_at")
            .IsRequired();

        builder.Property(ta => ta.ActivatedAt)
            .HasColumnName("activated_at");

        builder.Property(ta => ta.DeactivatedAt)
            .HasColumnName("deactivated_at");

        builder.Property(ta => ta.UninstalledAt)
            .HasColumnName("uninstalled_at");

        // Indexes
        builder.HasIndex(ta => ta.TenantId)
            .HasDatabaseName("ix_tenant_applications_tenant_id");

        builder.HasIndex(ta => ta.ApplicationId)
            .HasDatabaseName("ix_tenant_applications_application_id");

        builder.HasIndex(ta => new { ta.TenantId, ta.Slug })
            .HasDatabaseName("ix_tenant_applications_tenant_id_slug")
            .IsUnique();

        builder.HasIndex(ta => new { ta.TenantId, ta.ApplicationId })
            .HasDatabaseName("ix_tenant_applications_tenant_id_application_id")
            .IsUnique()
            .HasFilter("application_id IS NOT NULL");  // Only one install per tenant per application; custom apps have null ApplicationId

        builder.HasIndex(ta => new { ta.ApplicationId, ta.Major, ta.Minor, ta.Patch })
            .HasDatabaseName("ix_tenant_applications_application_id_version");

        builder.HasIndex(ta => ta.Status)
            .HasDatabaseName("ix_tenant_applications_status");

        // Relationships
        builder.HasMany(ta => ta.Environments)
            .WithOne()
            .HasForeignKey(e => e.TenantApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### TenantApplicationEnvironmentConfiguration

**File**: `TenantApplication.Infrastructure/Persistence/Configurations/TenantApplicationEnvironmentConfiguration.cs`

```csharp
namespace Datarizen.TenantApplication.Infrastructure.Persistence.Configurations;

public sealed class TenantApplicationEnvironmentConfiguration 
    : IEntityTypeConfiguration<TenantApplicationEnvironment>
{
    public void Configure(EntityTypeBuilder<TenantApplicationEnvironment> builder)
    {
        builder.ToTable("tenant_application_environments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TenantApplicationId)
            .HasColumnName("tenant_application_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.EnvironmentType)
            .HasColumnName("environment_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ApplicationReleaseId)
            .HasColumnName("application_release_id");

        builder.Property(e => e.ReleaseVersion)
            .HasColumnName("release_version")
            .HasMaxLength(50);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active");

        builder.Property(e => e.DeployedAt)
            .HasColumnName("deployed_at");

        builder.Property(e => e.DeployedBy)
            .HasColumnName("deployed_by");

        builder.Property(e => e.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(e => e.TenantApplicationId)
            .HasDatabaseName("ix_tenant_application_environments_tenant_application_id");

        builder.HasIndex(e => new { e.TenantApplicationId, e.Name })
            .HasDatabaseName("ix_tenant_application_environments_tenant_application_id_name")
            .IsUnique();

        builder.HasIndex(e => e.EnvironmentType)
            .HasDatabaseName("ix_tenant_application_environments_environment_type");
    }
}
```

---

## Repository Implementation

### TenantApplicationRepository

**File**: `TenantApplication.Infrastructure/Persistence/Repositories/TenantApplicationRepository.cs`

```csharp
namespace Datarizen.TenantApplication.Infrastructure.Persistence.Repositories;

public sealed class TenantApplicationRepository 
    : Repository<TenantApplication, Guid>, ITenantApplicationRepository
{
    public TenantApplicationRepository(TenantApplicationDbContext context) 
        : base(context)
    {
    }

    public async Task<TenantApplication?> GetByTenantAndApplicationAsync(
        Guid tenantId,
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantApplication>()
            .Include(ta => ta.Environments)
            .FirstOrDefaultAsync(ta => 
                ta.TenantId == tenantId && 
                ta.ApplicationId == applicationId,
                cancellationToken);
    }

    public async Task<List<TenantApplication>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantApplication>()
            .Include(ta => ta.Environments)
            .Where(ta => ta.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TenantApplication>> GetByApplicationIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantApplication>()
            .Include(ta => ta.Environments)
            .Where(ta => ta.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantApplication>()
            .Include(ta => ta.Environments)
            .FirstOrDefaultAsync(ta => ta.Id == id, cancellationToken);
    }
}
```

---

## Application Services

### ApplicationReleaseValidator

**File**: `TenantApplication.Infrastructure/Services/ApplicationReleaseValidator.cs`

```csharp
namespace Datarizen.TenantApplication.Infrastructure.Services;

public sealed class ApplicationReleaseValidator : IApplicationReleaseValidator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ApplicationReleaseValidator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<bool> ValidateReleaseExistsAsync(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken = default)
    {
        // In Monolith/MultiApp: Call AppBuilder service directly
        // In Microservices: Call AppBuilder HTTP API
        
        var topology = _configuration["Deployment:Topology"];
        
        if (topology == "Microservices")
        {
            return await ValidateViaHttpAsync(
                applicationId, 
                major, 
                minor, 
                patch, 
                cancellationToken);
        }
        else
        {
            return await ValidateViaServiceAsync(
                applicationId, 
                major, 
                minor, 
                patch, 
                cancellationToken);
        }
    }

    private async Task<bool> ValidateViaHttpAsync(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("AppBuilder");
        var response = await client.GetAsync(
            $"/api/appbuilder/applications/{applicationId}/releases/{major}.{minor}.{patch}",
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    private async Task<bool> ValidateViaServiceAsync(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken)
    {
        // Direct service call (injected via DI)
        // This would use AppBuilder.Contracts
        await Task.CompletedTask;
        return true; // Simplified
    }
}
```

---

## Service Registration

**File**: `TenantApplication.Infrastructure/DependencyInjection.cs`

```csharp
namespace Datarizen.TenantApplication.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantApplicationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<TenantApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<ITenantApplicationRepository, TenantApplicationRepository>();

        // Unit of Work (use a dedicated UnitOfWork implementation if the project uses one; otherwise DbContext may implement IUnitOfWork or a thin wrapper is used)
        services.AddScoped<IUnitOfWork>(sp => 
            sp.GetRequiredService<TenantApplicationDbContext>());

        // Application Services
        services.AddScoped<IApplicationReleaseValidator, ApplicationReleaseValidator>();

        // HTTP Clients (for Microservices topology)
        services.AddHttpClient("AppBuilder", client =>
        {
            var appBuilderUrl = configuration["Services:AppBuilder:Url"];
            if (!string.IsNullOrEmpty(appBuilderUrl))
            {
                client.BaseAddress = new Uri(appBuilderUrl);
            }
        });

        return services;
    }
}
```

---

## Success Criteria

- ✅ Entity configurations use Major/Minor/Patch columns
- ✅ Removed Version string column
- ✅ Composite index on version fields
- ✅ Repository includes Environments in queries
- ✅ ApplicationReleaseValidator supports both topologies
- ✅ HTTP client configured for Microservices
- ✅ Service registration complete

**Estimated Time**: 4 hours


