# Release, Installation, Deployment & Migration Process

**Status**: 🆕 Updated - Complete Process Documentation  
**Last Updated**: 2025-01-15  
**Module Ownership**: AppBuilder, TenantApplication, AppRuntime  

---

## Overview

This document describes the complete lifecycle of applications in Datarizen:

1. **Release** - Create immutable version of application
2. **Installation** - Add application to tenant catalog
3. **Deployment** - Activate release in specific environment
4. **Migration** - Upgrade schema between releases
5. **Testing** - Test before release (platform and tenant)

**Key Principles**:
- ✅ Releases are immutable (cannot modify after creation)
- ✅ Installation ≠ Deployment (install creates record, deploy activates)
- ✅ Environment isolation (Dev/Staging/Prod can have different releases)
- ✅ Migration tracking (all schema changes versioned)
- ✅ Dual schema support (appbuilder + tenantapplication)
- ✅ Testing before release (both platform and tenant)

---

## Module Responsibilities

| Module | Responsibility | Entities |
|--------|---------------|----------|
| **AppBuilder** | Platform-level releases, definitions | ApplicationDefinition, ApplicationRelease, EntityDefinition, NavigationDefinition, PageDefinition, DataSourceDefinition |
| **TenantApplication** | Tenant-level releases, installations, deployments, migrations | TenantApplication, TenantApplicationEnvironment, TenantApplicationMigration, Tenant* entities (when AppBuilder feature enabled) |
| **AppRuntime** | Runtime instances, engine management | RuntimeInstance, NavigationEngineVersion, PageEngineVersion, DataSourceEngineVersion, ComponentEngineMapping |

---

## 1. Release Process

### Platform Application Release (AppBuilder Module)

**Who**: Platform developers  
**Where**: `appbuilder` schema  
**Purpose**: Create immutable version of platform application  

**Entities Involved**:
- `ApplicationDefinition` (Draft → Released)
- `ApplicationRelease` (Created)
- `EntityDefinition`, `NavigationDefinition`, `PageDefinition`, `DataSourceDefinition` (Read for snapshot)

**Flow**:

```
1. Platform developer creates ApplicationDefinition
   ├─ Name = "CRM System"
   ├─ Slug = "crm"
   ├─ Status = Draft
   └─ IsPublic = false

2. Developer designs application via AppBuilder
   ├─ Add entities (Customer, Lead, Opportunity)
   ├─ Add pages (Dashboard, Customer List, Lead Form)
   ├─ Add navigation (Main Menu, Sidebar)
   └─ Add data sources (PostgreSQL, REST API)

3. Developer triggers release
   ├─ System validates draft
   ├─ System creates ApplicationRelease
   │  ├─ Version = "1.0.0"
   │  ├─ Major = 1, Minor = 0, Patch = 0
   │  ├─ ReleaseNotes = "Initial release"
   │  ├─ NavigationJson = JSON snapshot of navigation
   │  ├─ PageJson = JSON snapshot of pages
   │  ├─ DataSourceJson = JSON snapshot of data sources
   │  ├─ EntityJson = JSON snapshot of entities
   │  └─ IsActive = true
   ├─ System creates ComponentEngineMapping (AppRuntime)
   │  ├─ Map each NavigationDefinition → NavigationEngineVersion
   │  ├─ Map each PageDefinition → PageEngineVersion
   │  └─ Map each DataSourceDefinition → DataSourceEngineVersion
   └─ System updates ApplicationDefinition
      ├─ CurrentVersion = "1.0.0"
      └─ Status = Released

4. Release is now available in tenant catalog
```

**Command Handler**: `CreateApplicationReleaseCommandHandler` (AppBuilder.Application)  
**Domain Logic**: `ApplicationDefinition.CreateRelease()` (AppBuilder.Domain)

---

### Tenant Custom Application Release (TenantApplication Module)

**Who**: Tenant users (requires AppBuilder feature)  
**Where**: `tenantapplication` schema  
**Purpose**: Create immutable version of custom application  

**Entities Involved**:
- `TenantApplication` (Draft → Released)
- `TenantApplicationRelease` (Created in tenantapplication schema)
- `TenantEntityDefinition`, `TenantNavigationDefinition`, `TenantPageDefinition`, `TenantDataSourceDefinition` (Read for snapshot)

**Flow**:

```
1. Tenant creates custom TenantApplication
   ├─ TenantId = current tenant
   ├─ Name = "Custom Inventory"
   ├─ Slug = "inventory"
   ├─ IsCustom = true
   ├─ Status = Draft
   └─ ApplicationReleaseId = NULL

2. Tenant designs application via the AppBuilder UX, which calls **TenantApplication** API (definition CRUD in `tenantapplication` schema), not AppBuilder API
   ├─ Add entities, pages, navigation, data sources
   └─ Store in tenantapplication schema (tenant_* tables)

3. Tenant triggers release
   ├─ System validates draft
   ├─ System creates TenantApplicationRelease
   │  ├─ Version = "1.0.0"
   │  ├─ Major = 1, Minor = 0, Patch = 0
   │  ├─ ReleaseNotes = "Initial release"
   │  ├─ NavigationJson = JSON snapshot
   │  ├─ PageJson = JSON snapshot
   │  ├─ DataSourceJson = JSON snapshot
   │  ├─ EntityJson = JSON snapshot
   │  └─ IsActive = true
   ├─ System creates ComponentEngineMapping (AppRuntime)
   └─ System updates TenantApplication
      ├─ ApplicationReleaseId = new release
      └─ Status = Released

4. Release is now available for deployment
```

**Command Handler**: `CreateTenantApplicationReleaseCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplication.CreateRelease()` (TenantApplication.Domain)

---

## 2. Installation Process

### Install Platform Application (TenantApplication Module)

**Who**: Tenant users  
**Where**: `tenantapplication` schema  
**Purpose**: Add platform application to tenant catalog  

**Entities Involved**:
- `TenantApplication` (Created)
- `TenantApplicationEnvironment` (Created for Development)

**Flow**:

```
1. Tenant browses platform application catalog
2. Tenant clicks "Install" on ApplicationRelease
3. System validates prerequisites
   ├─ Check if tenant already has this application
   ├─ Check if tenant has required permissions
   └─ If validation fails → Block installation
4. System creates TenantApplication
   ├─ TenantId = current tenant
   ├─ ApplicationReleaseId = selected release
   ├─ Name = "CRM System" (from release)
   ├─ Slug = "crm" (from release)
   ├─ IsCustom = false
   ├─ SourceApplicationReleaseId = NULL
   └─ Status = Installed
5. System auto-creates Development environment
   ├─ TenantApplicationId = new application
   ├─ EnvironmentType = Development
   ├─ ApplicationReleaseId = NULL (not deployed yet)
   ├─ IsActive = false
   └─ DeployedAt = NULL
6. Tenant can now deploy to Development
```

**Command Handler**: `InstallPlatformApplicationCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplication.InstallFromPlatform()` (TenantApplication.Domain)

---

### Fork Platform Application (TenantApplication Module)

**Who**: Tenant users (requires AppBuilder feature)  
**Where**: `tenantapplication` schema  
**Purpose**: Customize platform application  

**Entities Involved**:
- `TenantApplication` (Created with IsCustom = true)
- `Tenant*` entities (Copied from source release)

**Flow**:

```
1. Tenant has installed platform ApplicationRelease
2. Tenant clicks "Customize Application"
3. System validates AppBuilder feature is enabled
4. System creates new TenantApplication
   ├─ TenantId = current tenant
   ├─ ApplicationReleaseId = NULL (not released yet)
   ├─ IsCustom = true
   ├─ SourceApplicationReleaseId = original platform release
   ├─ Slug = user-provided
   └─ Status = Draft
5. System copies all definitions from SourceApplicationRelease
   ├─ Copy entities → TenantEntityDefinition
   ├─ Copy properties → TenantPropertyDefinition
   ├─ Copy relations → TenantRelationDefinition
   ├─ Copy navigation → TenantNavigationDefinition
   ├─ Copy pages → TenantPageDefinition
   └─ Copy data sources → TenantDataSourceDefinition
6. Tenant modifies entities, pages, navigation
7. Tenant releases custom TenantApplicationRelease
8. Tenant deploys custom release to environments
```

**Command Handler**: `ForkPlatformApplicationCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplication.ForkFromPlatform()` (TenantApplication.Domain)

---

## 3. Deployment Process

### Deploy to Development (TenantApplication + AppRuntime Modules)

**Who**: Tenant users  
**Where**: `tenantapplication` schema  
**Purpose**: Activate release in Development environment  

**Entities Involved**:
- `TenantApplicationEnvironment` (Updated)
- `RuntimeInstance` (Created by AppRuntime module)
- `ComponentEngineMapping` (Read for compatibility check)

**Flow**:

```
1. Tenant selects ApplicationRelease to deploy
2. System validates compatibility (AppRuntime module)
   ├─ Load ComponentEngineMapping for release
   ├─ Check if NavigationEngineVersion exists and is active
   ├─ Check if PageEngineVersion exists and is active
   ├─ Check if DataSourceEngineVersion exists and is active
   └─ If incompatible → Block deployment
3. System updates TenantApplicationEnvironment
   ├─ EnvironmentType = Development
   ├─ ApplicationReleaseId = selected release
   ├─ ReleaseVersion = "1.0.0"
   ├─ DeployedAt = now
   ├─ DeployedBy = current user
   └─ IsActive = true
4. System raises domain event: ReleaseDeployedEvent
5. AppRuntime module handles event and creates RuntimeInstance
   ├─ TenantApplicationEnvironmentId = environment ID
   ├─ ApplicationReleaseId = selected release
   ├─ NavigationEngineVersionId = from ComponentEngineMapping
   ├─ PageEngineVersionId = from ComponentEngineMapping
   ├─ DataSourceEngineVersionId = from ComponentEngineMapping
   └─ Status = Running
6. Application is now accessible at:
   /{tenantSlug}/{appSlug}/development
```

**Command Handler**: `DeployApplicationCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplicationEnvironment.DeployRelease()` (TenantApplication.Domain)  
**Event Handler**: `ReleaseDeployedEventHandler` (AppRuntime.Application)  
**Domain Logic**: `RuntimeInstance.Create()` (AppRuntime.Domain)

---

### Deploy to Staging (TenantApplication + AppRuntime Modules)

**Who**: Tenant users  
**Where**: `tenantapplication` schema  
**Purpose**: Promote release to Staging for QA  

**Flow**:

```
1. Tenant selects ApplicationRelease to deploy
2. System validates prerequisites
   ├─ Development environment must exist
   ├─ Development environment must be active
   └─ If not met → Block deployment
3. System validates compatibility (AppRuntime)
4. System creates/updates Staging environment
5. System raises ReleaseDeployedEvent
6. AppRuntime module creates RuntimeInstance for Staging
7. Application is now accessible at:
   /{tenantSlug}/{appSlug}/staging
```

**Command Handler**: `DeployApplicationCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplicationEnvironment.DeployRelease()` (TenantApplication.Domain)

---

### Deploy to Production (TenantApplication + AppRuntime Modules)

**Who**: Tenant users  
**Where**: `tenantapplication` schema  
**Purpose**: Make release available to end users  

**Flow**:

```
1. Tenant selects ApplicationRelease to deploy
2. System validates prerequisites
   ├─ Staging environment must exist
   ├─ Staging environment must be active
   ├─ Staging must have same or newer release
   └─ If not met → Block deployment
3. System validates compatibility (AppRuntime)
4. System generates migration script (if schema changed)
5. System creates TenantApplicationMigration
6. System executes migration
7. System creates/updates Production environment
8. System raises ReleaseDeployedEvent
9. AppRuntime module creates RuntimeInstance for Production
10. Application is now accessible at:
    /{tenantSlug}/{appSlug} (default)
    /{tenantSlug}/{appSlug}/production (explicit)
```

**Command Handler**: `DeployApplicationCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplicationEnvironment.DeployRelease()` (TenantApplication.Domain)

---

## 4. Migration Process

### Schema Migration (TenantApplication Module)

**Who**: System (automated) or Tenant users (manual trigger)  
**Where**: `tenantapplication` schema  
**Purpose**: Upgrade database schema when deploying new release  

**Entities Involved**:
- `TenantApplicationMigration` (Created)
- `ApplicationRelease` (Read for schema comparison)

**Flow**:

```
1. Tenant deploys new ApplicationRelease (e.g., v1.1.0)
2. System detects schema changes
   ├─ Compare EntityJson (v1.0.0 vs v1.1.0)
   ├─ Detect added/removed/modified entities
   ├─ Detect added/removed/modified properties
   └─ Detect added/removed/modified relations
3. System generates migration script
   ├─ ALTER TABLE statements
   ├─ CREATE TABLE statements
   ├─ DROP TABLE statements
   ├─ Data migration scripts (if needed)
   └─ Store as JSON in TenantApplicationMigration
4. System creates TenantApplicationMigration
   ├─ TenantApplicationEnvironmentId = environment ID
   ├─ FromReleaseId = v1.0.0
   ├─ ToReleaseId = v1.1.0
   ├─ MigrationScriptJson = generated script
   ├─ Status = Pending
   ├─ ExecutedAt = NULL
   └─ ErrorMessage = NULL
5. System executes migration (or waits for manual approval)
   ├─ Status = Running
   ├─ Execute SQL statements via DatabaseMigrations capability
   ├─ If success → Status = Completed, ExecutedAt = now
   └─ If failure → Status = Failed, ErrorMessage = error details
6. If migration succeeds, deployment continues
7. If migration fails, deployment is rolled back
```

**Command Handler**: `ExecuteSchemaMigrationCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplicationMigration.Execute()` (TenantApplication.Domain)  
**Infrastructure**: `DatabaseMigrationService` (Capabilities/DatabaseMigrations)

---

## 5. Testing Before Release

### Platform-Level Testing (AppBuilder Module)

**Who**: Platform developers  
**Where**: `appbuilder` schema  
**Purpose**: Test draft applications before release  

**Flow**:

```
1. Create ApplicationDefinition (Draft status)
2. Design entities, pages, navigation, data sources
3. Create temporary "preview" ApplicationRelease
   ├─ Version = "0.0.0-preview"
   ├─ IsActive = false (not visible in tenant catalog)
   └─ Only accessible to platform developers
4. Deploy preview release to test environment
5. Test functionality, fix bugs
6. Delete preview release
7. Create official ApplicationRelease (e.g., "1.0.0")
```

**Command Handler**: `CreatePreviewReleaseCommandHandler` (AppBuilder.Application)

---

### Tenant-Level Testing (TenantApplication Module)

**Who**: Tenant users  
**Where**: `tenantapplication` schema  
**Purpose**: Test custom applications before production  

**Flow**:

```
1. Create TenantApplication (IsCustom = true, Status = Draft)
2. Design entities, pages, navigation, data sources
3. Deploy draft to Development environment
   ├─ No release needed for Development
   ├─ Development can deploy draft applications
   ├─ Use TenantApplicationEnvironment.DeployDraft()
   └─ Test functionality, fix bugs
4. When ready, create TenantApplicationRelease
5. Deploy release to Staging → Production
```

**Command Handler**: `DeployDraftCommandHandler` (TenantApplication.Application)  
**Domain Logic**: `TenantApplicationEnvironment.DeployDraft()` (TenantApplication.Domain)

---

## 6. Complete Workflow Examples

### Example 1: Platform Application Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│ PLATFORM DEVELOPER (AppBuilder Module)                          │
└─────────────────────────────────────────────────────────────────┘

1. Create ApplicationDefinition "CRM System"
   └─ Status = Draft

2. Design application
   ├─ Add EntityDefinition: Customer, Lead, Opportunity
   ├─ Add PropertyDefinition: firstName, lastName, email, etc.
   ├─ Add RelationDefinition: Customer → Opportunities (OneToMany)
   ├─ Add NavigationDefinition: Main Menu, Sidebar
   ├─ Add PageDefinition: Dashboard, Customer List, Lead Form
   └─ Add DataSourceDefinition: PostgreSQL, REST API

3. Test draft (optional)
   ├─ Create preview ApplicationRelease "0.0.0-preview"
   ├─ Deploy to test environment
   ├─ Test functionality
   └─ Delete preview release

4. Create official ApplicationRelease "1.0.0"
   ├─ System snapshots all definitions to JSON
   ├─ System creates ComponentEngineMapping
   │  ├─ NavigationDefinition → NavigationEngineVersion v1
   │  ├─ PageDefinition → PageEngineVersion v1
   │  └─ DataSourceDefinition → DataSourceEngineVersion v1
   └─ Status = Released, IsActive = true

5. Release is now available in tenant catalog

┌─────────────────────────────────────────────────────────────────┐
│ TENANT USER (TenantApplication Module)                          │
└─────────────────────────────────────────────────────────────────┘

6. Install ApplicationRelease "CRM System v1.0.0"
   ├─ System creates TenantApplication
   │  ├─ ApplicationReleaseId = CRM v1.0.0
   │  ├─ IsCustom = false
   │  └─ Status = Installed
   └─ System creates Development environment (not deployed)

7. Deploy to Development
   ├─ System validates compatibility (AppRuntime)
   ├─ System updates TenantApplicationEnvironment
   ├─ System raises ReleaseDeployedEvent
   ├─ AppRuntime creates RuntimeInstance
   └─ Application accessible at: /acme-corp/crm/development

8. Test in Development
   └─ Fix any issues, report bugs to platform

9. Deploy to Staging
   ├─ System validates Development is active
   ├─ System creates Staging environment
   ├─ AppRuntime creates RuntimeInstance
   └─ Application accessible at: /acme-corp/crm/staging

10. QA testing in Staging
    └─ Approve for production

11. Deploy to Production
    ├─ System validates Staging is active
    ├─ System creates Production environment
    ├─ AppRuntime creates RuntimeInstance
    └─ Application accessible at: /acme-corp/crm

┌─────────────────────────────────────────────────────────────────┐
│ PLATFORM DEVELOPER (AppBuilder Module) - New Version            │
└─────────────────────────────────────────────────────────────────┘

12. Update ApplicationDefinition "CRM System"
    ├─ Add new EntityDefinition: Invoice
    ├─ Add new PageDefinition: Invoice List
    └─ Modify existing PageDefinition: Dashboard (add invoice widget)

13. Create ApplicationRelease "1.1.0"
    ├─ System snapshots updated definitions
    ├─ System creates ComponentEngineMapping
    │  ├─ NavigationDefinition → NavigationEngineVersion v1 (unchanged)
    │  ├─ PageDefinition → PageEngineVersion v2 (new features)
    │  └─ DataSourceDefinition → DataSourceEngineVersion v1 (unchanged)
    └─ Release available in catalog

┌─────────────────────────────────────────────────────────────────┐
│ TENANT USER (TenantApplication Module) - Upgrade                │
└─────────────────────────────────────────────────────────────────┘

14. Deploy new release to Development
    ├─ System detects schema changes (new Invoice entity)
    ├─ System generates migration script
    ├─ System creates TenantApplicationMigration
    ├─ System executes migration (CREATE TABLE invoices)
    ├─ System updates environment to v1.1.0
    └─ Application accessible at: /acme-corp/crm/development

15. Test new features in Development

16. Deploy to Staging
    ├─ System executes migration
    └─ Application accessible at: /acme-corp/crm/staging

17. Deploy to Production
    ├─ System executes migration
    └─ Application accessible at: /acme-corp/crm
```

---

### Example 2: Custom Application Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│ TENANT USER (TenantApplication Module) - Custom App             │
│ Requires: AppBuilder feature enabled                            │
└─────────────────────────────────────────────────────────────────┘

1. Create custom TenantApplication "Inventory System"
   ├─ TenantId = acme-corp
   ├─ ApplicationReleaseId = NULL
   ├─ IsCustom = true
   ├─ Status = Draft
   └─ Slug = "inventory"

2. Design application (stored in tenantapplication schema)
   ├─ Add TenantEntityDefinition: Product, Warehouse, Stock
   ├─ Add TenantPropertyDefinition: sku, name, quantity, etc.
   ├─ Add TenantRelationDefinition: Warehouse → Stock (OneToMany)
   ├─ Add TenantNavigationDefinition: Main Menu
   ├─ Add TenantPageDefinition: Product List, Stock Report
   └─ Add TenantDataSourceDefinition: PostgreSQL

3. Deploy draft to Development (no release needed)
   ├─ System creates Development environment
   ├─ System deploys draft definitions
   ├─ AppRuntime creates RuntimeInstance
   └─ Application accessible at: /acme-corp/inventory/development

4. Test in Development
   └─ Iterate on design, fix bugs

5. Create TenantApplicationRelease "1.0.0"
   ├─ System snapshots all tenant definitions to JSON
   ├─ System creates ComponentEngineMapping
   └─ TenantApplication.ApplicationReleaseId = new release

6. Deploy release to Staging
   ├─ System validates Development is active
   ├─ System creates Staging environment
   └─ Application accessible at: /acme-corp/inventory/staging

7. Deploy release to Production
   ├─ System validates Staging is active
   ├─ System creates Production environment
   └─ Application accessible at: /acme-corp/inventory

8. Update custom application
   ├─ Add new TenantEntityDefinition: Supplier
   ├─ Modify TenantPageDefinition: Product List (add supplier column)
   └─ Status = Draft (again)

9. Deploy draft to Development
   └─ Test changes

10. Create TenantApplicationRelease "1.1.0"
    ├─ System detects schema changes
    └─ System creates ComponentEngineMapping

11. Deploy to Staging → Production
    ├─ System generates migration script
    ├─ System executes migration
    └─ Application upgraded
```

---

### Example 3: Fork Platform Application

```
┌─────────────────────────────────────────────────────────────────┐
│ TENANT USER (TenantApplication Module) - Fork & Customize       │
│ Requires: AppBuilder feature enabled                            │
└─────────────────────────────────────────────────────────────────┘

1. Tenant has installed "CRM System v1.0.0"
   └─ Deployed to Production

2. Tenant clicks "Customize Application"
   ├─ System validates AppBuilder feature is enabled
   └─ System creates new TenantApplication
      ├─ ApplicationReleaseId = NULL
      ├─ IsCustom = true
      ├─ SourceApplicationReleaseId = CRM v1.0.0
      ├─ Slug = "custom-crm"
      └─ Status = Draft

3. System copies all definitions from source
   ├─ Copy EntityDefinition → TenantEntityDefinition
   │  ├─ Customer, Lead, Opportunity
   │  └─ All properties and relations
   ├─ Copy NavigationDefinition → TenantNavigationDefinition
   ├─ Copy PageDefinition → TenantPageDefinition
   └─ Copy DataSourceDefinition → TenantDataSourceDefinition

4. Tenant modifies definitions
   ├─ Add new TenantEntityDefinition: Contract
   ├─ Add new TenantPropertyDefinition: contractValue, startDate
   ├─ Modify TenantPageDefinition: Dashboard (add contract widget)
   └─ Remove TenantPageDefinition: Lead Form (not needed)

5. Deploy draft to Development
   └─ Test customizations

6. Create TenantApplicationRelease "1.0.0-custom"
   └─ System snapshots customized definitions

7. Deploy to Staging → Production
   ├─ System generates migration script
   ├─ System executes migration
   └─ Custom CRM accessible at: /acme-corp/custom-crm

8. Tenant now has TWO CRM applications
   ├─ Original: /acme-corp/crm (platform version)
   └─ Custom: /acme-corp/custom-crm (forked + customized)
```

---

## 7. Environment Management

### Environment Lifecycle

```
Development Environment
├─ Purpose: Active development and testing
├─ Can deploy: Draft applications OR Releases
├─ Migration: Auto-executed
├─ Rollback: Allowed
└─ URL: /{tenantSlug}/{appSlug}/development

Staging Environment
├─ Purpose: QA and pre-production testing
├─ Can deploy: Releases only (no drafts)
├─ Migration: Auto-executed
├─ Rollback: Allowed
├─ Prerequisite: Development must exist and be active
└─ URL: /{tenantSlug}/{appSlug}/staging

Production Environment
├─ Purpose: End-user access
├─ Can deploy: Releases only (no drafts)
├─ Migration: Manual approval required (optional)
├─ Rollback: Requires backup restore
├─ Prerequisite: Staging must exist and be active
└─ URL: /{tenantSlug}/{appSlug} (default)
     /{tenantSlug}/{appSlug}/production (explicit)
```

---

### Environment Configuration

Each environment can have independent configuration:

```json
{
  "database": {
    "connectionString": "Host=dev-db;Database=acme_crm_dev",
    "poolSize": 10
  },
  "features": {
    "enableDebugMode": true,
    "enableTestData": true
  },
  "integrations": {
    "emailProvider": "SendGrid-Test",
    "paymentGateway": "Stripe-Test"
  }
}
```

**Stored in**: `TenantApplicationEnvironment.ConfigurationJson`

---

## 8. Migration Strategy

### Schema Change Detection

```
System compares EntityJson between releases:

v1.0.0 EntityJson:
{
  "entities": [
    {
      "name": "Customer",
      "properties": [
        { "name": "firstName", "type": "String" },
        { "name": "lastName", "type": "String" }
      ]
    }
  ]
}

v1.1.0 EntityJson:
{
  "entities": [
    {
      "name": "Customer",
      "properties": [
        { "name": "firstName", "type": "String" },
        { "name": "lastName", "type": "String" },
        { "name": "email", "type": "String" }  // NEW
      ]
    },
    {
      "name": "Invoice",  // NEW ENTITY
      "properties": [
        { "name": "invoiceNumber", "type": "String" },
        { "name": "amount", "type": "Number" }
      ]
    }
  ]
}

Generated Migration:
{
  "operations": [
    {
      "type": "AddColumn",
      "table": "customers",
      "column": "email",
      "dataType": "VARCHAR(255)",
      "nullable": true
    },
    {
      "type": "CreateTable",
      "table": "invoices",
      "columns": [
        { "name": "id", "type": "UUID", "primaryKey": true },
        { "name": "invoice_number", "type": "VARCHAR(50)" },
        { "name": "amount", "type": "DECIMAL(18,2)" }
      ]
    }
  ]
}
```

---

### Migration Execution

```
1. System creates TenantApplicationMigration
   ├─ FromReleaseId = v1.0.0
   ├─ ToReleaseId = v1.1.0
   ├─ MigrationScriptJson = generated script
   └─ Status = Pending

2. System validates migration
   ├─ Check for breaking changes
   ├─ Estimate execution time
   └─ Validate data integrity

3. System executes migration
   ├─ BEGIN TRANSACTION
   ├─ Execute ALTER TABLE customers ADD COLUMN email
   ├─ Execute CREATE TABLE invoices
   ├─ Verify schema changes
   ├─ COMMIT TRANSACTION
   └─ Status = Completed

4. If migration fails
   ├─ ROLLBACK TRANSACTION
   ├─ Status = Failed
   ├─ ErrorMessage = error details
   └─ Block deployment
```

---

### Rollback Strategy

**Development/Staging**:
```
1. Identify previous release
2. Create reverse migration
3. Execute rollback migration
4. Update environment to previous release
5. Restart RuntimeInstance
```

**Production**:
```
1. Stop RuntimeInstance
2. Restore database from backup
3. Update environment to previous release
4. Restart RuntimeInstance
5. Verify application health
```

---

## 9. Key Design Decisions

### Why ApplicationReleaseId is Nullable in TenantApplication?

✅ **Allows custom applications from scratch**
- Tenant creates TenantApplication with `ApplicationReleaseId = NULL`
- Tenant designs application using AppBuilder
- Tenant creates TenantApplicationRelease
- `ApplicationReleaseId` is set when release is created

✅ **Supports draft deployments**
- Development environment can deploy drafts (no release needed)
- Staging/Production require releases

---

### Why Dual Schema Support (appbuilder + tenantapplication)?

✅ **Platform isolation**
- Platform applications in `appbuilder` schema
- Tenant applications in `tenantapplication` schema
- No cross-contamination

✅ **Tenant isolation**
- Each tenant's custom apps are isolated
- Tenant A cannot see Tenant B's custom apps

✅ **Same domain logic**
- Reuse AppBuilder domain entities
- Different storage location
- Configuration-driven schema selection

---

### Why Table Name Prefix (tenant_)?

✅ **Clarity**
- `applications` (platform) vs `tenant_applications` (tenant)
- Clear distinction in database

✅ **Avoid conflicts**
- No naming collisions
- Easier to understand queries

✅ **Migration safety**
- Platform migrations don't affect tenant tables
- Tenant migrations don't affect platform tables

---

### Why Environment-Based Deployment?

✅ **Risk mitigation**
- Test in Development before Staging
- Test in Staging before Production
- Catch issues early

✅ **Independent releases**
- Development can have v1.2.0
- Staging can have v1.1.0
- Production can have v1.0.0

✅ **Gradual rollout**
- Deploy to Development → test
- Deploy to Staging → QA
- Deploy to Production → users

---

### Why Migration Tracking?

✅ **Audit trail**
- Know when schema changed
- Know who triggered migration
- Know what changed

✅ **Rollback support**
- Reverse migrations
- Restore from backup

✅ **Debugging**
- Identify when issues started
- Correlate with schema changes

---

## 10. API Endpoints

### AppBuilder Module

```
POST   /api/appbuilder/applications                    # Create draft
PUT    /api/appbuilder/applications/{id}               # Update draft
POST   /api/appbuilder/applications/{id}/release       # Create release
GET    /api/appbuilder/applications/{id}/releases      # List releases
POST   /api/appbuilder/applications/{id}/preview       # Create preview release
DELETE /api/appbuilder/applications/{id}/preview       # Delete preview release
```

### TenantApplication Module

```
# Installation
POST   /api/tenantapplications/install                 # Install platform app
POST   /api/tenantapplications/fork                    # Fork platform app
POST   /api/tenantapplications/create                  # Create custom app

# Management
GET    /api/tenantapplications                         # List tenant apps
GET    /api/tenantapplications/{id}                    # Get tenant app
PUT    /api/tenantapplications/{id}                    # Update tenant app
DELETE /api/tenantapplications/{id}                    # Delete tenant app

# Releases
POST   /api/tenantapplications/{id}/release            # Create release
GET    /api/tenantapplications/{id}/releases           # List releases

# Deployment
POST   /api/tenantapplications/{id}/deploy             # Deploy to environment
GET    /api/tenantapplications/{id}/environments       # List environments
POST   /api/tenantapplications/{id}/environments/{env}/deploy-draft  # Deploy draft (Dev only)

# Migration
GET    /api/tenantapplications/{id}/migrations         # List migrations
POST   /api/tenantapplications/{id}/migrations/{migId}/execute  # Execute migration
POST   /api/tenantapplications/{id}/migrations/{migId}/rollback # Rollback migration
```

### AppRuntime Module

```
GET    /api/appruntime/instances                       # List runtime instances
GET    /api/appruntime/instances/{id}                  # Get runtime instance
POST   /api/appruntime/instances/{id}/start            # Start instance
POST   /api/appruntime/instances/{id}/stop             # Stop instance
POST   /api/appruntime/instances/{id}/restart          # Restart instance
GET    /api/appruntime/instances/{id}/health           # Health check

GET    /api/appruntime/engines/navigation              # List navigation engines
GET    /api/appruntime/engines/page                    # List page engines
GET    /api/appruntime/engines/datasource              # List datasource engines
```

---

## Summary

| Process | Module | Schema | Purpose |
|---------|--------|--------|---------|
| **Release** | AppBuilder | `appbuilder` | Create immutable platform application version |
| **Release** | TenantApplication | `tenantapplication` | Create immutable custom application version |
| **Installation** | TenantApplication | `tenantapplication` | Add platform app to tenant catalog |
| **Fork** | TenantApplication | `tenantapplication` | Customize platform app |
| **Deployment** | TenantApplication | `tenantapplication` | Activate release in environment |
| **Migration** | TenantApplication | `tenantapplication` | Upgrade schema between releases |
| **Runtime** | AppRuntime | `appruntime` | Execute application with versioned engines |

**Key Principles**:
- ✅ Releases are immutable
- ✅ Installation ≠ Deployment
- ✅ Environment isolation (Dev/Staging/Prod)
- ✅ Migration tracking and rollback
- ✅ Dual schema support (platform + tenant)
- ✅ Testing before release
- ✅ Versioned execution engines
- ✅ Compatibility validation
