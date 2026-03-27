# AppBuilder Module - Infrastructure Layer

**Status**: ✅ Updated to align with new domain model  
**Last Updated**: 2026-02-11  
**Module**: AppBuilder  
**Layer**: Infrastructure  

---

## Overview

The Infrastructure layer implements DbContext, entity configurations, and repositories.

**Key Changes from Domain Update**:
- ❌ Removed ApplicationSchema table
- ✅ Added entity_definitions, property_definitions, relation_definitions tables
- ✅ Renamed *_components tables to *_definitions
- ✅ Split snapshot_json into navigation_json, page_json, datasource_json, entity_json
- ✅ Added major, minor, patch columns to application_releases
- ✅ Removed icon_url and created_by from application_definitions

---

## 1. Database Schema

### Tables

```
appbuilder schema:
├── application_definitions
├── application_releases
├── entity_definitions
├── property_definitions
├── relation_definitions
├── navigation_definitions
├── page_definitions
└── datasource_definitions
```

---

## 2. Entity Configurations

### 2.1 ApplicationDefinitionConfiguration

```csharp
public class ApplicationDefinitionConfiguration : IEntityTypeConfiguration<ApplicationDefinition>
{
    public void Configure(EntityTypeBuilder<ApplicationDefinition> builder)
    {
        builder.ToTable("application_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CurrentVersion).HasMaxLength(50);
        builder.Property(x => x.IsPublic).IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
```

---

### 2.2 EntityDefinitionConfiguration

```csharp
public class EntityDefinitionConfiguration : IEntityTypeConfiguration<EntityDefinition>
{
    public void Configure(EntityTypeBuilder<EntityDefinition> builder)
    {
        builder.ToTable("entity_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        
        builder.Property(x => x.Attributes)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.PrimaryKey).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<ApplicationDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => new { x.ApplicationId, x.Name }).IsUnique();
    }
}
```

---

### 2.3 PropertyDefinitionConfiguration

```csharp
public class PropertyDefinitionConfiguration : IEntityTypeConfiguration<PropertyDefinition>
{
    public void Configure(EntityTypeBuilder<PropertyDefinition> builder)
    {
        builder.ToTable("property_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DataType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsRequired).IsRequired();
        builder.Property(x => x.DefaultValue).HasMaxLength(500);
        
        builder.Property(x => x.ValidationRules)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<EntityDefinition>()
            .WithMany()
            .HasForeignKey(x => x.EntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => new { x.EntityId, x.Name }).IsUnique();
        builder.HasIndex(x => new { x.EntityId, x.Order });
    }
}
```

---

### 2.4 RelationDefinitionConfiguration

```csharp
public class RelationDefinitionConfiguration : IEntityTypeConfiguration<RelationDefinition>
{
    public void Configure(EntityTypeBuilder<RelationDefinition> builder)
    {
        builder.ToTable("relation_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceEntityId).IsRequired();
        builder.Property(x => x.TargetEntityId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        
        builder.Property(x => x.RelationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.CascadeDelete).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<EntityDefinition>()
            .WithMany()
            .HasForeignKey(x => x.SourceEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<EntityDefinition>()
            .WithMany()
            .HasForeignKey(x => x.TargetEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SourceEntityId);
        builder.HasIndex(x => x.TargetEntityId);
        builder.HasIndex(x => new { x.SourceEntityId, x.Name }).IsUnique();
    }
}
```

---

### 2.5 ApplicationReleaseConfiguration

```csharp
public class ApplicationReleaseConfiguration : IEntityTypeConfiguration<ApplicationRelease>
{
    public void Configure(EntityTypeBuilder<ApplicationRelease> builder)
    {
        builder.ToTable("application_releases", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationDefinitionId).IsRequired().HasColumnName("application_id");
        builder.Property(x => x.Major).IsRequired();
        builder.Property(x => x.Minor).IsRequired();
        builder.Property(x => x.Patch).IsRequired();
        builder.Property(x => x.ReleaseNotes).HasMaxLength(5000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.ReleasedAt).IsRequired();

        builder.Property(x => x.NavigationJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.PageJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.DataSourceJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.EntityJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasOne<ApplicationDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApplicationDefinitionId);
        builder.HasIndex(x => new { x.ApplicationDefinitionId, x.Major, x.Minor, x.Patch }).IsUnique();
        builder.HasIndex(x => new { x.ApplicationDefinitionId, x.IsActive });
    }
}
```

---

### 2.6 NavigationDefinitionConfiguration

```csharp
public class NavigationDefinitionConfiguration : IEntityTypeConfiguration<NavigationDefinition>
{
    public void Configure(EntityTypeBuilder<NavigationDefinition> builder)
    {
        builder.ToTable("navigation_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        
        builder.Property(x => x.ConfigurationJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<ApplicationDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApplicationId);
    }
}
```

---

### 2.7 PageDefinitionConfiguration

```csharp
public class PageDefinitionConfiguration : IEntityTypeConfiguration<PageDefinition>
{
    public void Configure(EntityTypeBuilder<PageDefinition> builder)
    {
        builder.ToTable("page_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Route).IsRequired().HasMaxLength(500);
        
        builder.Property(x => x.ConfigurationJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<ApplicationDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => new { x.ApplicationId, x.Route }).IsUnique();
    }
}
```

---

### 2.8 DataSourceDefinitionConfiguration

```csharp
public class DataSourceDefinitionConfiguration : IEntityTypeConfiguration<DataSourceDefinition>
{
    public void Configure(EntityTypeBuilder<DataSourceDefinition> builder)
    {
        builder.ToTable("datasource_definitions", "appbuilder");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        
        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.ConfigurationJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne<ApplicationDefinition>()
            .WithMany()
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApplicationId);
    }
}
```

---

## 3. Repository Interfaces

### 3.1 IEntityDefinitionRepository

```csharp
public interface IEntityDefinitionRepository : IRepository<EntityDefinition, Guid>
{
    Task<List<EntityDefinition>> GetByApplicationIdAsync(
        Guid applicationId, 
        CancellationToken cancellationToken = default);
    
    Task<EntityDefinition?> GetByNameAsync(
        Guid applicationId, 
        string name, 
        CancellationToken cancellationToken = default);
}
```

---

### 3.2 IPropertyDefinitionRepository

```csharp
public interface IPropertyDefinitionRepository : IRepository<PropertyDefinition, Guid>
{
    Task<List<PropertyDefinition>> GetByEntityIdAsync(
        Guid entityId, 
        CancellationToken cancellationToken = default);
    
    Task<PropertyDefinition?> GetByNameAsync(
        Guid entityId, 
        string name, 
        CancellationToken cancellationToken = default);
}
```

---

### 3.3 IRelationDefinitionRepository

```csharp
public interface IRelationDefinitionRepository : IRepository<RelationDefinition, Guid>
{
    Task<List<RelationDefinition>> GetByEntityIdAsync(
        Guid entityId, 
        CancellationToken cancellationToken = default);
    
    Task<List<RelationDefinition>> GetBySourceEntityIdAsync(
        Guid sourceEntityId, 
        CancellationToken cancellationToken = default);
    
    Task<List<RelationDefinition>> GetByTargetEntityIdAsync(
        Guid targetEntityId, 
        CancellationToken cancellationToken = default);
}
```

---

### 3.4 IApplicationReleaseRepository

```csharp
public interface IApplicationReleaseRepository : IRepository<ApplicationRelease, Guid>
{
    Task<List<ApplicationRelease>> GetByApplicationIdAsync(
        Guid applicationId, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationRelease?> GetActiveByApplicationIdAsync(
        Guid applicationId, 
        CancellationToken cancellationToken = default);
    
    Task<ApplicationRelease?> GetByVersionAsync(
        Guid applicationId, 
        int major, 
        int minor, 
        int patch, 
        CancellationToken cancellationToken = default);
}
```

---

## Success Criteria

- ✅ All entity configurations updated
- ✅ Removed ApplicationSchema configuration
- ✅ Added EntityDefinition, PropertyDefinition, RelationDefinition configurations
- ✅ Renamed *Component to *Definition
- ✅ Split SnapshotJson into 4 JSON columns
- ✅ Added Major/Minor/Patch columns
- ✅ Removed IconUrl and CreatedBy columns
- ✅ All repositories defined
- ✅ All indexes created
- ✅ All foreign keys configured

---

## DbContext

### AppBuilderDbContext

**File**: `AppBuilder.Infrastructure/Persistence/AppBuilderDbContext.cs`

```csharp
public class AppBuilderDbContext : DbContext
{
    public DbSet<ApplicationDefinition> ApplicationDefinitions => Set<ApplicationDefinition>();
    public DbSet<ApplicationRelease> ApplicationReleases => Set<ApplicationRelease>();
    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<PropertyDefinition> PropertyDefinitions => Set<PropertyDefinition>();
    public DbSet<RelationDefinition> RelationDefinitions => Set<RelationDefinition>();
    public DbSet<NavigationDefinition> NavigationDefinitions => Set<NavigationDefinition>();
    public DbSet<PageDefinition> PageDefinitions => Set<PageDefinition>();
    public DbSet<DataSourceDefinition> DataSourceDefinitions => Set<DataSourceDefinition>();

    public AppBuilderDbContext(DbContextOptions<AppBuilderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("appbuilder");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppBuilderDbContext).Assembly);
    }
}
```

---

## Dependency Injection

**File**: `AppBuilder.Infrastructure/DependencyInjection.cs`

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddAppBuilderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required");

        services.AddDbContext<AppBuilderDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "appbuilder")));

        services.AddScoped<IAppBuilderUnitOfWork, AppBuilderUnitOfWork>();
        services.AddScoped<IApplicationDefinitionRepository, ApplicationDefinitionRepository>();
        services.AddScoped<IApplicationReleaseRepository, ApplicationReleaseRepository>();
        services.AddScoped<IEntityDefinitionRepository, EntityDefinitionRepository>();
        services.AddScoped<IPropertyDefinitionRepository, PropertyDefinitionRepository>();
        services.AddScoped<IRelationDefinitionRepository, RelationDefinitionRepository>();
        services.AddScoped<INavigationDefinitionRepository, NavigationDefinitionRepository>();
        services.AddScoped<IPageDefinitionRepository, PageDefinitionRepository>();
        services.AddScoped<IDataSourceDefinitionRepository, DataSourceDefinitionRepository>();

        return services;
    }
}
```

---

## File Structure

```
AppBuilder.Infrastructure/
├── Persistence/
│   ├── AppBuilderDbContext.cs
│   ├── AppBuilderUnitOfWork.cs
│   └── Configurations/
│       ├── ApplicationDefinitionConfiguration.cs
│       ├── ApplicationReleaseConfiguration.cs
│       ├── EntityDefinitionConfiguration.cs
│       ├── PropertyDefinitionConfiguration.cs
│       ├── RelationDefinitionConfiguration.cs
│       ├── NavigationDefinitionConfiguration.cs
│       ├── PageDefinitionConfiguration.cs
│       └── DataSourceDefinitionConfiguration.cs
├── Repositories/
│   ├── ApplicationDefinitionRepository.cs
│   ├── ApplicationReleaseRepository.cs
│   ├── EntityDefinitionRepository.cs
│   ├── PropertyDefinitionRepository.cs
│   ├── RelationDefinitionRepository.cs
│   ├── NavigationDefinitionRepository.cs
│   ├── PageDefinitionRepository.cs
│   └── DataSourceDefinitionRepository.cs
└── DependencyInjection.cs
```

---

## Next Steps

1. ✅ Implement all repository classes
2. ✅ Implement all EF Core configurations
3. ✅ Create AppBuilderDbContext
4. ✅ Create AppBuilderUnitOfWork
5. ✅ Add DependencyInjection registration
6. ✅ Create initial migration
7. ✅ Test repository methods
8. ✅ Test EF Core configurations




