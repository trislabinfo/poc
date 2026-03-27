# Database Migration Process — Target Architecture & Refactoring Plan

**Document Purpose:** Define the target database migration process for both AppBuilder (platform applications) and TenantApplication modules, describe required refactoring, and outline the implementation plan using enterprise-grade libraries.

**Audience:** Architecture, development team, product.

**Status:** Planning / Refactoring Required

---

## Table of Contents

1. [Target Process Overview](#target-process-overview)
2. [AppBuilder Release Process](#appbuilder-release-process)
3. [TenantApplication Release & Deployment Process](#tenantapplication-release--deployment-process)
4. [Current Implementation Gaps](#current-implementation-gaps)
5. [Refactoring Plan](#refactoring-plan)
6. [Technology Stack & Libraries](#technology-stack--libraries)
7. [Architecture & Code Organization](#architecture--code-organization)
8. [Migration Strategy](#migration-strategy)

---

## Target Process Overview

### Core Principles

1. **DDL Scripts as First-Class Citizens**: Every release (platform or tenant) generates complete DDL scripts for all entities/properties/relations, stored in the database for review and modification.

2. **Full Schema Per Release**: Each release contains **all** DDL scripts needed to create the database schema from scratch (not incremental). This ensures:
   - Platform applications with many releases: each release is self-contained
   - Tenant applications: can be deployed to any environment independently
   - Rollback capability: can revert to any previous release

3. **Schema Comparison Per Environment**: When deploying to an existing environment, compare the **actual database schema** with the **target release schema** to generate a diff migration script.

4. **Review & Approval Workflow**: All generated DDL scripts and migration diffs must be:
   - Stored in the database
   - Reviewable via API/UI
   - Modifiable before approval
   - Approved before execution

5. **Pluggable Implementation**: Core interfaces allow swapping implementations (e.g., EF Core's `IMigrationsModelDiffer`, FluentMigrator, custom solutions).

---

## AppBuilder Release Process

### Flow

```
1. Developer creates/modifies Entity/Property/Relation definitions
   ↓
2. Developer creates Release (major.minor.patch)
   ↓
3. System generates DDL scripts for ALL entities/properties/relations
   ↓
4. DDL scripts stored in ApplicationRelease.DdlScriptsJson (or separate table)
   ↓
5. Developer reviews/modifies DDL scripts via API
   ↓
6. Developer approves release → Release.Status = Approved
   ↓
7. Release becomes available in catalog for tenant installation
```

### Requirements

- **Complete DDL Scripts**: Each release must contain DDL to create the entire schema (CREATE TABLE, ALTER TABLE, CREATE INDEX, FOREIGN KEYS, etc.)
- **Script Storage**: Store DDL scripts in `ApplicationRelease` entity (new field: `DdlScriptsJson` or `DdlScripts` table)
- **Review & Edit**: API endpoints to:
  - `GET /api/appbuilder/releases/{id}/ddl-scripts` - View scripts
  - `PUT /api/appbuilder/releases/{id}/ddl-scripts` - Update scripts
  - `POST /api/appbuilder/releases/{id}/approve` - Approve release
- **Versioning**: Each release version is immutable once approved; modifications create a new release

### Database Schema Changes

**New Fields in `ApplicationRelease`**:
```sql
ALTER TABLE appbuilder.application_releases
  ADD COLUMN ddl_scripts_json JSONB NOT NULL DEFAULT '{}',
  ADD COLUMN ddl_scripts_status VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
  ADD COLUMN approved_at TIMESTAMP NULL,
  ADD COLUMN approved_by UUID NULL;
```

**Or Separate Table** (preferred for large scripts):
```sql
CREATE TABLE appbuilder.release_ddl_scripts (
  id UUID PRIMARY KEY,
  release_id UUID NOT NULL REFERENCES appbuilder.application_releases(id),
  script_type VARCHAR(50) NOT NULL, -- 'CreateTables', 'CreateIndexes', 'CreateForeignKeys', etc.
  script_content TEXT NOT NULL,
  execution_order INT NOT NULL,
  created_at TIMESTAMP NOT NULL,
  updated_at TIMESTAMP NULL
);
```

---

## TenantApplication Release & Deployment Process

### Flow

```
1. Tenant installs platform app OR creates custom app OR forks platform app
   ↓
2. Tenant modifies definitions (if custom/forked)
   ↓
3. Tenant creates Release
   ↓
4. System generates DDL scripts for ALL entities/properties/relations
   ↓
5. DDL scripts stored in TenantApplicationRelease.DdlScriptsJson
   ↓
6. Tenant reviews/modifies DDL scripts via API
   ↓
7. Tenant approves release → Release.Status = Approved
   ↓
8. Tenant creates Environment (if not exists) → Database provisioned
   ↓
9. Tenant deploys Release to Environment
   ↓
10. System compares actual DB schema (from environment) with target release schema
   ↓
11. System generates migration diff script
   ↓
12. Migration diff stored in TenantApplicationMigration.MigrationScriptJson
   ↓
13. Tenant reviews/modifies migration script via API
   ↓
14. Tenant approves migration → Migration.Status = Approved
   ↓
15. Tenant promotes migration → Migration executed against environment DB
   ↓
16. Environment.ApplicationReleaseId updated → Deployment complete
```

### Detailed Steps

#### Step 1-7: Release Creation (Same as AppBuilder)

Tenant applications follow the same release process as platform applications:
- Generate complete DDL scripts
- Store for review
- Allow modification
- Require approval

#### Step 8: Environment Creation

When creating an environment:
- Generate database name: `{tenant-slug}-{app-slug}-{env-name}`
- Provision PostgreSQL database
- Store connection string in `TenantApplicationEnvironment.ConnectionString`
- **Do NOT** apply any schema yet (empty database)

#### Step 9: Deploy Release to Environment

When deploying a release to an environment:

**Case A: Environment has no deployed release (first deployment)**
- Apply complete DDL scripts from release
- Update `Environment.ApplicationReleaseId`
- Mark migration as completed

**Case B: Environment has existing deployed release**
- Load current release schema (from `Environment.ApplicationReleaseId`)
- Load target release schema (from deployment request)
- Compare schemas → generate diff migration script
- Store diff in `TenantApplicationMigration` with status `Pending`
- Require review and approval before execution

#### Step 10-12: Schema Comparison & Diff Generation

**Compare Actual DB Schema vs Target Release Schema**:
1. Read actual database schema from environment's database (using EF Core's `IMigrationsModelDiffer` or database introspection)
2. Read target release schema (from `TenantApplicationRelease.DdlScriptsJson` or derived from release)
3. Use schema comparer to generate diff operations
4. Generate SQL migration script from diff operations
5. Store script in `TenantApplicationMigration.MigrationScriptJson`

**Schema Comparison Options**:
- **Option 1**: Compare actual DB schema (via `IMigrationsModelDiffer` reading from DB) vs release schema model
- **Option 2**: Compare previous release schema vs new release schema (if previous release was fully applied)
- **Option 3**: Hybrid - compare both and merge results

**Recommended**: Option 1 (actual DB vs target release) for accuracy, with Option 2 as fallback.

#### Step 13-15: Review, Approve, Promote

- **Review**: Tenant views migration script via API
- **Modify**: Tenant can update script via `PUT /api/tenantapplication/.../migrations/{id}`
- **Approve**: Tenant approves script → `Migration.Status = Approved`
- **Promote**: Tenant executes migration → `Migration.Status = Executing` → `Completed` or `Failed`

---

## Current Implementation Gaps

### What Exists Today

✅ **Basic Infrastructure**:
- `ApplicationRelease` entity (stores definition JSON)
- `TenantApplicationRelease` entity
- `TenantApplicationMigration` entity
- `TenantApplicationEnvironment` entity (with `DatabaseName`, `ConnectionString`)
- Custom `SchemaDeriver` (converts definitions to schema model)
- Custom `SchemaComparer` (compares schema models)
- Custom `SqlMigrationScriptGenerator` (generates SQL from changes)
- `IDatabaseProvisioner` (creates databases)
- `IMigrationExecutor` (executes migration scripts)

❌ **Missing Critical Features**:

1. **DDL Script Generation & Storage**:
   - No DDL script generation during release creation
   - No storage of DDL scripts in `ApplicationRelease`
   - No review/approval workflow for DDL scripts

2. **Schema Comparison with Actual Database**:
   - Current `SchemaComparer` only compares schema models (not actual DB)
   - No way to read actual database schema from environment
   - No comparison between actual DB and target release schema

3. **Complete Schema Per Release**:
   - Current implementation stores schema as JSON (`SchemaJson`)
   - No DDL scripts stored
   - No guarantee that release contains complete schema

4. **Review & Approval Workflow**:
   - No approval status for releases
   - No approval status for migrations
   - No API endpoints for reviewing/modifying scripts

5. **Enterprise-Grade Libraries**:
   - Custom schema comparison (should use EF Core's `IMigrationsModelDiffer`)
   - Custom SQL generation (should use EF Core's `MigrationsSqlGenerator`)
   - No validation against actual database schemas

---

## Refactoring Plan

### Phase 1: Create Shared Capability for Schema Management

**New Project**: `Capabilities.DatabaseSchema`

**Purpose**: Shared code for schema derivation, comparison, and DDL generation used by both AppBuilder and TenantApplication modules.

**Structure**:
```
/server/src/Capabilities/DatabaseSchema
  /Capabilities.DatabaseSchema.csproj
  /Abstractions
    ISchemaDeriver.cs                    # Derive schema from definitions
    ISchemaComparer.cs                   # Compare schemas (pluggable)
    IDdlScriptGenerator.cs               # Generate DDL scripts
    IDatabaseSchemaReader.cs             # Read schema from actual database
    IMigrationScriptGenerator.cs         # Generate migration scripts from diffs
  /EfCore                                # EF Core implementations
    EfCoreSchemaDeriver.cs
    EfCoreSchemaComparer.cs              # Uses IMigrationsModelDiffer
    EfCoreDdlScriptGenerator.cs          # Uses MigrationsSqlGenerator
    EfCoreDatabaseSchemaReader.cs        # Reads from actual DB
    EfCoreMigrationScriptGenerator.cs
  /Models
    DatabaseSchema.cs                    # Schema model (tables, columns, FKs)
    SchemaChangeSet.cs                   # Diff operations
    DdlScript.cs                         # DDL script model
  /Extensions
    DatabaseSchemaServiceCollectionExtensions.cs
```

**Key Interfaces**:

```csharp
// Derive schema from entity/property/relation definitions
public interface ISchemaDeriver
{
    Task<DatabaseSchema> DeriveSchemaAsync(
        IReadOnlyList<EntityDefinition> entities,
        Dictionary<Guid, List<PropertyDefinition>> propertiesByEntityId,
        IReadOnlyList<RelationDefinition> relations,
        CancellationToken cancellationToken = default);
}

// Compare two schemas (pluggable - EF Core, custom, etc.)
public interface ISchemaComparer
{
    Task<SchemaChangeSet> CompareAsync(
        DatabaseSchema sourceSchema,
        DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default);
    
    // Compare actual database schema with target schema
    Task<SchemaChangeSet> CompareWithDatabaseAsync(
        string connectionString,
        DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default);
}

// Generate complete DDL scripts for a schema
public interface IDdlScriptGenerator
{
    Task<DdlScript> GenerateDdlScriptAsync(
        DatabaseSchema schema,
        CancellationToken cancellationToken = default);
}

// Read schema from actual database
public interface IDatabaseSchemaReader
{
    Task<DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}

// Generate migration script from change set
public interface IMigrationScriptGenerator
{
    Task<string> GenerateMigrationScriptAsync(
        SchemaChangeSet changeSet,
        DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default);
}
```

### Phase 2: Update ApplicationRelease Entity

**Add DDL Script Storage**:

```csharp
public sealed class ApplicationRelease : Entity<Guid>
{
    // ... existing fields ...
    
    // New fields for DDL scripts
    public string DdlScriptsJson { get; private set; } = "{}";
    public DdlScriptStatus DdlScriptsStatus { get; private set; } = DdlScriptStatus.Pending;
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    
    // Methods
    public void SetDdlScripts(string ddlScriptsJson)
    {
        DdlScriptsJson = ddlScriptsJson ?? "{}";
        DdlScriptsStatus = DdlScriptStatus.Pending;
    }
    
    public void ApproveDdlScripts(Guid approvedBy, IDateTimeProvider dateTimeProvider)
    {
        if (DdlScriptsStatus != DdlScriptStatus.Pending)
            throw new InvalidOperationException("Only pending scripts can be approved.");
        DdlScriptsStatus = DdlScriptStatus.Approved;
        ApprovedAt = dateTimeProvider.UtcNow;
        ApprovedBy = approvedBy;
    }
}

public enum DdlScriptStatus
{
    Pending,
    Approved,
    Rejected
}
```

**Migration**:
```sql
ALTER TABLE appbuilder.application_releases
  ADD COLUMN ddl_scripts_json JSONB NOT NULL DEFAULT '{}',
  ADD COLUMN ddl_scripts_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
  ADD COLUMN approved_at TIMESTAMP NULL,
  ADD COLUMN approved_by UUID NULL;

ALTER TABLE tenantapplication.tenant_application_releases
  ADD COLUMN ddl_scripts_json JSONB NOT NULL DEFAULT '{}',
  ADD COLUMN ddl_scripts_status VARCHAR(50) NOT NULL DEFAULT 'Pending',
  ADD COLUMN approved_at TIMESTAMP NULL,
  ADD COLUMN approved_by UUID NULL;
```

### Phase 3: Refactor Release Creation Handlers

**AppBuilder: `CreateApplicationReleaseCommandHandler`**:

```csharp
public async Task<Result<Guid>> Handle(CreateApplicationReleaseCommand request, CancellationToken cancellationToken)
{
    // ... existing code to load entities/properties/relations ...
    
    // 1. Derive schema from definitions
    var schema = await _schemaDeriver.DeriveSchemaAsync(entityList, propertiesByEntityId, relationSnapshots, cancellationToken);
    
    // 2. Generate complete DDL scripts
    var ddlScript = await _ddlScriptGenerator.GenerateDdlScriptAsync(schema, cancellationToken);
    var ddlScriptsJson = JsonSerializer.Serialize(ddlScript);
    
    // 3. Create release with DDL scripts
    var releaseResult = app.CreateRelease(
        version, major, minor, patch,
        releaseNotes ?? string.Empty,
        navigationJson, pageJson, dataSourceJson, entityJson,
        ddlScriptsJson,  // NEW: DDL scripts
        releasedBy,
        _dateTimeProvider);
    
    // ... rest of handler ...
}
```

**TenantApplication: `CreateTenantApplicationReleaseCommandHandler`**:

Same pattern - generate DDL scripts and store in release.

### Phase 4: Add DDL Script Review API

**New Endpoints**:

```
GET  /api/appbuilder/releases/{id}/ddl-scripts
PUT  /api/appbuilder/releases/{id}/ddl-scripts
POST /api/appbuilder/releases/{id}/approve

GET  /api/tenantapplication/tenants/{tenantId}/applications/{id}/releases/{releaseId}/ddl-scripts
PUT  /api/tenantapplication/tenants/{tenantId}/applications/{id}/releases/{releaseId}/ddl-scripts
POST /api/tenantapplication/tenants/{tenantId}/applications/{id}/releases/{releaseId}/approve
```

### Phase 5: Refactor Schema Comparison for Environment Deployment

**New Handler: `DeployToEnvironmentCommandHandler`**:

```csharp
public async Task<Result> Handle(DeployToEnvironmentCommand request, CancellationToken cancellationToken)
{
    var environment = await _envRepository.GetByIdAsync(request.EnvironmentId, cancellationToken);
    var targetRelease = await _releaseRepository.GetByIdAsync(request.ReleaseId, cancellationToken);
    
    // Check if environment has existing deployment
    if (environment.ApplicationReleaseId.HasValue)
    {
        // Compare actual DB schema with target release schema
        var actualSchema = await _databaseSchemaReader.ReadSchemaAsync(
            environment.ConnectionString, cancellationToken);
        
        var targetSchema = await LoadSchemaFromReleaseAsync(targetRelease, cancellationToken);
        
        // Generate diff
        var changeSet = await _schemaComparer.CompareAsync(
            actualSchema, targetSchema, cancellationToken);
        
        // Generate migration script
        var migrationScript = await _migrationScriptGenerator.GenerateMigrationScriptAsync(
            changeSet, targetSchema, cancellationToken);
        
        // Create migration record
        var migration = TenantApplicationMigration.Create(
            environment.Id,
            environment.ApplicationReleaseId,
            request.ReleaseId,
            migrationScript);
        
        await _migrationRepository.AddAsync(migration, cancellationToken);
        
        // Return migration ID for review
        return Result.Success(migration.Id);
    }
    else
    {
        // First deployment - apply complete DDL scripts
        await ApplyDdlScriptsAsync(environment, targetRelease, cancellationToken);
        environment.DeployRelease(request.ReleaseId, targetRelease.Version, request.DeployedBy, _dateTimeProvider);
        _envRepository.Update(environment);
    }
    
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    return Result.Success();
}
```

### Phase 6: Update Migration Approval & Execution

**Add Approval Status to `TenantApplicationMigration`**:

```csharp
public enum MigrationStatus
{
    Pending,      // Created, awaiting review
    Approved,     // Reviewed and approved
    Executing,    // Currently running
    Completed,    // Successfully executed
    Failed        // Execution failed
}

public void Approve(Guid approvedBy, IDateTimeProvider dateTimeProvider)
{
    if (Status != MigrationStatus.Pending)
        throw new InvalidOperationException("Only pending migrations can be approved.");
    Status = MigrationStatus.Approved;
    ApprovedAt = dateTimeProvider.UtcNow;
    ApprovedBy = approvedBy;
}
```

**New Endpoints**:

```
POST /api/tenantapplication/.../migrations/{id}/approve
POST /api/tenantapplication/.../migrations/{id}/promote  (execute)
```

---

## Technology Stack & Libraries

### Recommended: EF Core Native APIs

**Primary Choice**: Use EF Core's built-in migration APIs for schema comparison and SQL generation.

**Rationale**:
- ✅ Battle-tested by Microsoft
- ✅ No external dependencies
- ✅ Supports all EF Core database providers (PostgreSQL, SQL Server, etc.)
- ✅ Handles complex edge cases
- ✅ Well-documented and maintained
- ✅ Can be swapped with other implementations via interfaces

**Key APIs**:

1. **`IMigrationsModelDiffer`**: Compares two `IRelationalModel` instances
   ```csharp
   var differ = context.GetService<IMigrationsModelDiffer>();
   var operations = differ.GetDifferences(sourceModel, targetModel);
   ```

2. **`MigrationsSqlGenerator`**: Generates SQL from `MigrationOperation` objects
   ```csharp
   var generator = context.GetService<MigrationsSqlGenerator>();
   var commands = generator.Generate(operations, model);
   ```

3. **`IDesignTimeModel`**: Builds model from DbContext
   ```csharp
   var model = designTimeModel.Model;
   var relationalModel = model.GetRelationalModel();
   ```

**Alternative Libraries** (can be swapped later):

- **FluentMigrator**: If we want database-agnostic migrations with fluent API
- **DbUp**: If we prefer SQL-first approach
- **EfCore.SchemaCompare**: For validation (complementary, not replacement)

### Implementation Strategy

**Use EF Core APIs via Abstractions**:

```csharp
// Capabilities.DatabaseSchema/EfCore/EfCoreSchemaComparer.cs
public sealed class EfCoreSchemaComparer : ISchemaComparer
{
    private readonly IMigrationsModelDiffer _differ;
    private readonly IRelationalModelDependencies _dependencies;
    
    public async Task<SchemaChangeSet> CompareAsync(
        DatabaseSchema sourceSchema,
        DatabaseSchema targetSchema,
        CancellationToken cancellationToken)
    {
        // Convert DatabaseSchema to IRelationalModel
        var sourceModel = ConvertToRelationalModel(sourceSchema);
        var targetModel = ConvertToRelationalModel(targetSchema);
        
        // Use EF Core's differ
        var operations = _differ.GetDifferences(sourceModel, targetModel);
        
        // Convert MigrationOperations to SchemaChangeSet
        return ConvertToChangeSet(operations);
    }
}
```

**Benefits**:
- Core logic uses EF Core (battle-tested)
- Interfaces allow swapping implementations
- Testable via mocks
- Can add FluentMigrator implementation later if needed

---

## Architecture & Code Organization

### Shared Capability: `Capabilities.DatabaseSchema`

**Location**: `/server/src/Capabilities/DatabaseSchema`

**Dependencies**:
- `Microsoft.EntityFrameworkCore` (for EF Core implementations)
- `ApplicationDefinition.Domain` (for EntityDefinition, PropertyDefinition, etc.)
- `BuildingBlocks.Kernel` (for Result, IDateTimeProvider)

**Usage by Modules**:
- **AppBuilder**: Uses for release DDL generation
- **TenantApplication**: Uses for release DDL generation and environment schema comparison

### Module-Specific Code

**AppBuilder Module**:
- `AppBuilder.Application/Commands/CreateApplicationRelease/` - Uses `IDdlScriptGenerator`
- `AppBuilder.Application/Commands/ApproveRelease/` - Approves DDL scripts
- `AppBuilder.Api/Controllers/ReleasesController.cs` - DDL review endpoints

**TenantApplication Module**:
- `TenantApplication.Application/Commands/CreateTenantApplicationRelease/` - Uses `IDdlScriptGenerator`
- `TenantApplication.Application/Commands/DeployToEnvironment/` - Uses `ISchemaComparer`, `IDatabaseSchemaReader`
- `TenantApplication.Application/Commands/ApproveMigration/` - Approves migration scripts
- `TenantApplication.Application/Commands/PromoteMigration/` - Executes approved migrations

### Code Removal

**Delete Custom Implementations** (replace with EF Core-based):

- ❌ `ApplicationDefinition.Application/SchemaDerivation/SchemaDeriver.cs` → Use `Capabilities.DatabaseSchema/EfCore/EfCoreSchemaDeriver`
- ❌ `ApplicationDefinition.Application/SchemaComparison/SchemaComparer.cs` → Use `Capabilities.DatabaseSchema/EfCore/EfCoreSchemaComparer`
- ❌ `ApplicationDefinition.Application/MigrationScriptGeneration/SqlMigrationScriptGenerator.cs` → Use `Capabilities.DatabaseSchema/EfCore/EfCoreMigrationScriptGenerator`
- ❌ `ApplicationDefinition.Domain/Schema/*.cs` → Keep models, but move to `Capabilities.DatabaseSchema/Models`

**Keep Interfaces** (as abstractions):
- ✅ `ISchemaDeriver` → Move to `Capabilities.DatabaseSchema/Abstractions`
- ✅ `ISchemaComparer` → Move to `Capabilities.DatabaseSchema/Abstractions`
- ✅ `ISqlMigrationScriptGenerator` → Rename to `IMigrationScriptGenerator`, move to `Capabilities.DatabaseSchema/Abstractions`

---

## Migration Strategy

### Step 1: Create `Capabilities.DatabaseSchema` Project

1. Create new project
2. Add EF Core dependencies
3. Define interfaces (abstractions)
4. Implement EF Core-based classes
5. Add service registration extensions

### Step 2: Update ApplicationRelease Entity

1. Add `DdlScriptsJson`, `DdlScriptsStatus`, `ApprovedAt`, `ApprovedBy` fields
2. Create migration for schema changes
3. Update domain methods

### Step 3: Refactor Release Handlers

1. Update `CreateApplicationReleaseCommandHandler` (AppBuilder)
2. Update `CreateTenantApplicationReleaseCommandHandler` (TenantApplication)
3. Generate DDL scripts during release creation
4. Store scripts in release entity

### Step 4: Add Review & Approval APIs

1. Add DDL script review endpoints
2. Add approval endpoints
3. Add migration approval endpoints

### Step 5: Refactor Deployment Logic

1. Update `DeployToEnvironmentCommandHandler`
2. Implement schema comparison with actual database
3. Generate migration diffs
4. Require approval before execution

### Step 6: Remove Old Code

1. Delete custom schema comparison implementations
2. Update all references to use new capability
3. Run tests to ensure everything works

### Step 7: Testing

1. Unit tests for schema derivation
2. Unit tests for schema comparison
3. Integration tests for release creation with DDL generation
4. Integration tests for deployment with schema comparison
5. End-to-end tests for full workflow

---

## Testing Strategy

### Unit Tests

**Test Schema Derivation**:
```csharp
[Fact]
public async Task DeriveSchema_WithEntitiesAndProperties_ReturnsCorrectSchema()
{
    // Arrange
    var entities = new[] { /* ... */ };
    var properties = new Dictionary<Guid, List<PropertyDefinition>>();
    var relations = new List<RelationDefinition>();
    
    // Act
    var schema = await _schemaDeriver.DeriveSchemaAsync(entities, properties, relations);
    
    // Assert
    Assert.Equal(expectedTableCount, schema.Tables.Count);
    Assert.Contains("users", schema.Tables.Select(t => t.Name));
}
```

**Test Schema Comparison**:
```csharp
[Fact]
public async Task CompareSchemas_WithAddedTable_ReturnsTableAddedOperation()
{
    // Arrange
    var sourceSchema = CreateSourceSchema();
    var targetSchema = CreateTargetSchemaWithNewTable();
    
    // Act
    var changeSet = await _schemaComparer.CompareAsync(sourceSchema, targetSchema);
    
    // Assert
    Assert.Contains(changeSet.TableChanges, c => 
        c.ChangeType == TableChangeType.Added && c.TableName == "new_table");
}
```

### Integration Tests

**Test Release Creation with DDL Generation**:
```csharp
[Fact]
public async Task CreateRelease_GeneratesDdlScripts()
{
    // Arrange
    var command = new CreateApplicationReleaseCommand(/* ... */);
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    var release = await _releaseRepository.GetByIdAsync(result.Value);
    Assert.NotEmpty(release.DdlScriptsJson);
    Assert.Equal(DdlScriptStatus.Pending, release.DdlScriptsStatus);
}
```

**Test Deployment with Schema Comparison**:
```csharp
[Fact]
public async Task DeployToEnvironment_WithExistingDeployment_CreatesMigration()
{
    // Arrange
    var environment = CreateEnvironmentWithDeployedRelease();
    var newRelease = CreateNewRelease();
    
    // Act
    var result = await _deployHandler.Handle(
        new DeployToEnvironmentCommand(environment.Id, newRelease.Id), 
        CancellationToken.None);
    
    // Assert
    var migration = await _migrationRepository.GetByEnvironmentAsync(environment.Id);
    Assert.NotNull(migration);
    Assert.NotEmpty(migration.MigrationScriptJson);
    Assert.Equal(MigrationStatus.Pending, migration.Status);
}
```

---

## Summary

### Key Changes

1. **DDL Scripts**: Every release generates complete DDL scripts, stored in database for review
2. **Review Workflow**: All scripts require review and approval before execution
3. **Schema Comparison**: Compare actual database schema with target release schema
4. **Shared Capability**: `Capabilities.DatabaseSchema` provides shared logic for both modules
5. **EF Core APIs**: Use `IMigrationsModelDiffer` and `MigrationsSqlGenerator` for enterprise-grade comparison and SQL generation
6. **Pluggable Design**: Interfaces allow swapping implementations (EF Core, FluentMigrator, custom)

### Benefits

- ✅ Enterprise-grade schema comparison (EF Core)
- ✅ Complete DDL scripts per release (self-contained)
- ✅ Review and approval workflow (governance)
- ✅ Actual database schema comparison (accuracy)
- ✅ Shared code between modules (DRY)
- ✅ Testable architecture (interfaces, mocks)
- ✅ Future-proof (can swap implementations)

### Next Steps

1. Create `Capabilities.DatabaseSchema` project
2. Implement EF Core-based schema comparison
3. Update `ApplicationRelease` entity
4. Refactor release creation handlers
5. Add review/approval APIs
6. Refactor deployment logic
7. Remove old custom code
8. Add comprehensive tests

---

**Document Version**: 1.0  
**Last Updated**: 2026-02-16  
**Author**: Architecture Team
