# End-to-End Test Flow — Complete Database Migration Process

This document describes the complete test flow from AppBuilder application creation through TenantApplication deployment with database provisioning and migrations.

**Status:** ✅ All endpoints implemented and ready for testing

---

## Prerequisites

1. **Database Server**: PostgreSQL server running (single server for all databases)
2. **Tenant Created**: At least one tenant exists in the system
3. **API Access**: Access to AppBuilder and TenantApplication API endpoints

---

## Complete Test Flow

### Phase 1: AppBuilder — Create Platform Application

#### Step 1.1: Create Application Definition

**Endpoint**: `POST /api/appbuilder/application-definitions`

**Request Body**:
```json
{
  "name": "Customer Management",
  "description": "Manage customers and orders",
  "slug": "customer-management",
  "isPublic": true
}
```

**Expected Response**: `201 Created` with application ID

**Notes**: Creates a platform application in Draft status.

---

#### Step 1.2: Create Entity Definitions

**Endpoints**:
- `POST /api/appbuilder/entities` - Create entities (e.g., Customer, Order)
- `POST /api/appbuilder/entities/{entityId}/properties` - Add properties to entities
- `POST /api/appbuilder/relations` - Create relationships between entities

**Example**: Create Customer entity with properties:
```json
POST /api/appbuilder/entities
{
  "applicationDefinitionId": "<app-id>",
  "name": "Customer",
  "displayName": "Customer",
  "primaryKey": "id"
}

POST /api/appbuilder/entities/<customer-entity-id>/properties
{
  "name": "id",
  "displayName": "Id",
  "dataType": 0,  // String
  "isRequired": true,
  "order": 0
}
```

---

#### Step 1.3: Create Release

**Endpoint**: `POST /api/appbuilder/applications/{applicationId}/releases`

**Request Body**:
```json
{
  "major": 1,
  "minor": 0,
  "patch": 0,
  "releaseNotes": "Initial release"
}
```

**Expected Response**: `201 Created` with release ID

**What Happens**:
- ✅ System derives schema from all entities/properties/relations
- ✅ System generates complete DDL scripts (CREATE TABLE, CREATE INDEX, FOREIGN KEYS)
- ✅ DDL scripts stored in `ApplicationRelease.DdlScriptsJson`
- ✅ Release status: `DdlScriptsStatus = Pending`

---

#### Step 1.4: Review DDL Scripts

**Endpoint**: `GET /api/appbuilder/releases/{releaseId}/ddl-scripts`

**Expected Response**: 
```json
{
  "ddlScriptsJson": "{ ... }",  // Complete DDL script object
  "ddlScriptsStatus": "Pending",
  "approvedAt": null,
  "approvedBy": null
}
```

**What to Check**:
- Verify all tables are created correctly
- Verify column types match expectations
- Verify foreign keys are correct

---

#### Step 1.5: Update DDL Scripts (Optional)

**Endpoint**: `PUT /api/appbuilder/applications/{applicationId}/releases/{releaseId}/ddl-scripts`

**Request Body**:
```json
{
  "ddlScriptsJson": "{ ... }"  // Modified DDL scripts
}
```

**Use Case**: Modify scripts if needed before approval.

---

#### Step 1.6: Approve Release

**Endpoint**: `POST /api/appbuilder/applications/{applicationId}/releases/{releaseId}/approve`

**Expected Response**: `200 OK`

**What Happens**:
- ✅ `DdlScriptsStatus` changes to `Approved`
- ✅ `ApprovedAt` and `ApprovedBy` are set
- ✅ Release becomes available in catalog

---

#### Step 1.7: Verify Release in Catalog

**Endpoint**: `GET /api/appbuilder/catalog/applications`

**Expected Response**: List of installable applications including the one just created

---

### Phase 2: TenantApplication — Install Platform Application

#### Step 2.1: Install Application from Catalog

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/install`

**Request Body**:
```json
{
  "applicationReleaseId": "<release-id-from-step-1.3>",
  "name": "Customer Management",
  "slug": "customer-management",
  "configurationJson": "{}"
}
```

**Expected Response**: `201 Created` with tenant application ID

**What Happens**:
- ✅ Creates `TenantApplication` record
- ✅ References platform release (`IsCustom = false`)
- ✅ Status: `Installed`

---

### Phase 3: TenantApplication — Create Release

#### Step 3.1: Create Release for Installed Application

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/releases`

**Request Body**:
```json
{
  "major": 1,
  "minor": 0,
  "patch": 0,
  "releaseNotes": "Tenant release"
}
```

**Expected Response**: `201 Created` with release ID

**What Happens**:
- ✅ System reads current definitions from tenant's definition tables
- ✅ Derives schema and generates DDL scripts
- ✅ Stores DDL scripts in `TenantApplicationRelease.DdlScriptsJson`
- ✅ Status: `DdlScriptsStatus = Pending`

**Note**: For installed apps, definitions should already exist (copied from platform or created manually).

---

#### Step 3.2: Review and Approve Tenant Release

**Endpoints**:
- `GET /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/releases/{releaseId}/ddl-scripts`
- `PUT /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/releases/{releaseId}/ddl-scripts` (optional)
- `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/releases/{releaseId}/approve`

**Expected Result**: Release approved, ready for deployment

---

### Phase 4: TenantApplication — Create Environment

#### Step 4.1: Create Environment

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments`

**Request Body**:
```json
{
  "name": "Development",
  "environmentType": 0  // 0=Development, 1=Staging, 2=Production
}
```

**Expected Response**: `201 Created` with environment ID

**What Happens**:
- ✅ Creates `TenantApplicationEnvironment` record
- ✅ **Creates new PostgreSQL database** with name: `{tenant-slug}-{app-slug}-{env-name}`
- ✅ Stores database name and connection string in environment
- ✅ Database is empty (no schema applied yet)

**Database Name Example**: `acme-corp-customer-management-development`

---

#### Step 4.2: Verify Environment Created

**Endpoint**: `GET /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}`

**Expected Response**: Environment details including `databaseName` and `connectionString`

---

### Phase 5: TenantApplication — Deploy Release

#### Step 5.1: Deploy Release to Environment (First Deployment)

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}/deploy`

**Request Body**:
```json
{
  "releaseId": "<release-id-from-step-3.1>",
  "version": "1.0.0"
}
```

**Expected Response**: `200 OK` with message "Deployment completed successfully."

**What Happens** (First Deployment):
- ✅ System loads DDL scripts from approved release
- ✅ Executes DDL scripts against environment's database
- ✅ Creates all tables, indexes, foreign keys
- ✅ Updates environment's `ApplicationReleaseId` and `ReleaseVersion`
- ✅ Sets environment as active

**Database State**: Environment database now has complete schema matching the release.

---

#### Step 5.2: Create New Release (for Migration Testing)

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/releases`

**Request Body**:
```json
{
  "major": 1,
  "minor": 1,
  "patch": 0,
  "releaseNotes": "Added Order entity"
}
```

**Steps**:
1. Add new entity/properties (e.g., Order entity)
2. Create release (generates new DDL scripts)
3. Review and approve release

---

#### Step 5.3: Deploy New Release (Creates Migration)

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}/deploy`

**Request Body**:
```json
{
  "releaseId": "<new-release-id>",
  "version": "1.1.0"
}
```

**Expected Response**: `200 OK` with migration ID:
```json
{
  "migrationId": "<migration-id>",
  "message": "Migration created. Please review and approve before execution."
}
```

**What Happens** (Subsequent Deployment):
- ✅ System reads actual database schema from environment's database
- ✅ Loads target release schema
- ✅ Compares schemas and generates diff
- ✅ Creates migration script from diff
- ✅ Stores migration in `TenantApplicationMigration` with status `Pending`
- ✅ **Does NOT** execute migration yet (requires approval)

---

### Phase 6: TenantApplication — Review and Execute Migration

#### Step 6.1: Review Migration Script

**Endpoint**: `GET /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}/migrations/{migrationId}`

**Expected Response**: Migration details including `MigrationScriptJson`

**What to Check**:
- Verify migration script contains expected changes
- Check for any unexpected operations

---

#### Step 6.2: Update Migration Script (Optional)

**Endpoint**: `PUT /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}/migrations/{migrationId}`

**Request Body**:
```json
{
  "migrationScriptJson": "-- Custom migration script\nALTER TABLE ..."
}
```

**Use Case**: Modify migration script if needed.

---

#### Step 6.3: Approve Migration

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}/migrations/{migrationId}/approve`

**Expected Response**: `200 OK`

**What Happens**:
- ✅ Migration status changes to `Approved`
- ✅ `ApprovedAt` and `ApprovedBy` are set
- ✅ Migration is ready for execution

---

#### Step 6.4: Execute Migration (Promote)

**Endpoint**: `POST /api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/environments/{environmentId}/migrations/{migrationId}/run`

**Expected Response**: `200 OK`

**What Happens**:
- ✅ Migration status changes to `Executing`
- ✅ SQL script executed against environment's database
- ✅ Migration status changes to `Completed` on success
- ✅ Environment's `ApplicationReleaseId` updated to new release
- ✅ Environment's `ReleaseVersion` updated

**Database State**: Environment database now matches the new release schema.

---

## Test Scenarios

### Scenario 1: Complete Flow (Happy Path)

1. Create platform app → Create entities → Create release → Approve release
2. Install app for tenant → Create tenant release → Approve tenant release
3. Create environment → Database created automatically
4. Deploy release → DDL scripts applied, schema created
5. Create new release → Deploy → Migration created
6. Review migration → Approve → Execute → Schema updated

**Expected Result**: ✅ All steps complete successfully

---

### Scenario 2: Multiple Environments

1. Create environment "Development" → Database: `{tenant-slug}-{app-slug}-development`
2. Create environment "Staging" → Database: `{tenant-slug}-{app-slug}-staging`
3. Create environment "Production" → Database: `{tenant-slug}-{app-slug}-production`

**Expected Result**: ✅ Three separate databases created

---

### Scenario 3: Multiple Tenant Applications

1. Tenant installs App A → Creates tenant app A
2. Tenant installs App B → Creates tenant app B
3. Create environment for App A → Database: `{tenant-slug}-app-a-development`
4. Create environment for App B → Database: `{tenant-slug}-app-b-development`

**Expected Result**: ✅ Separate databases for each app

---

### Scenario 4: Migration Rollback (Future)

**Note**: Rollback infrastructure is ready (can deploy previous release), but explicit rollback endpoint not yet implemented.

---

## Verification Checklist

### Database Creation
- [ ] Each environment has its own database
- [ ] Database name follows pattern: `{tenant-slug}-{app-slug}-{env-name}`
- [ ] Connection string stored in environment record
- [ ] Database is created on same server (as configured)

### DDL Script Generation
- [ ] Platform release generates complete DDL scripts
- [ ] Tenant release generates complete DDL scripts
- [ ] Scripts include all tables, columns, indexes, foreign keys
- [ ] Scripts are stored in database for review

### Schema Comparison
- [ ] Actual DB schema compared with target release schema
- [ ] Migration diff generated correctly
- [ ] Migration script contains only necessary changes

### Approval Workflow
- [ ] DDL scripts require approval before release is available
- [ ] Migrations require approval before execution
- [ ] Scripts can be modified before approval
- [ ] Approval status tracked correctly

### Migration Execution
- [ ] Only approved migrations can be executed
- [ ] Migration execution updates environment's deployed release
- [ ] Failed migrations are marked as failed
- [ ] Error messages stored for failed migrations

---

## API Endpoint Summary

### AppBuilder Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/appbuilder/application-definitions` | Create application |
| POST | `/api/appbuilder/applications/{id}/releases` | Create release (generates DDL) |
| GET | `/api/appbuilder/releases/{id}/ddl-scripts` | View DDL scripts |
| PUT | `/api/appbuilder/applications/{id}/releases/{id}/ddl-scripts` | Update DDL scripts |
| POST | `/api/appbuilder/applications/{id}/releases/{id}/approve` | Approve release |
| GET | `/api/appbuilder/catalog/applications` | List installable apps |

### TenantApplication Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/tenantapplication/tenants/{id}/applications/install` | Install platform app |
| POST | `/api/tenantapplication/tenants/{id}/applications/custom` | Create custom app |
| POST | `/api/tenantapplication/tenants/{id}/applications/fork` | Fork platform app |
| POST | `/api/tenantapplication/.../applications/{id}/releases` | Create release (generates DDL) |
| GET | `/api/tenantapplication/.../releases/{id}/ddl-scripts` | View DDL scripts |
| PUT | `/api/tenantapplication/.../releases/{id}/ddl-scripts` | Update DDL scripts |
| POST | `/api/tenantapplication/.../releases/{id}/approve` | Approve release |
| POST | `/api/tenantapplication/.../applications/{id}/environments` | Create environment (creates DB) |
| POST | `/api/tenantapplication/.../environments/{id}/deploy` | Deploy release |
| GET | `/api/tenantapplication/.../migrations/{id}` | View migration |
| PUT | `/api/tenantapplication/.../migrations/{id}` | Update migration script |
| POST | `/api/tenantapplication/.../migrations/{id}/approve` | Approve migration |
| POST | `/api/tenantapplication/.../migrations/{id}/run` | Execute migration |

---

## Database Naming Convention

**Pattern**: `{tenant-slug}-{tenantapplication-slug}-{environment-name}`

**Examples**:
- `acme-corp-customer-management-development`
- `acme-corp-customer-management-staging`
- `acme-corp-customer-management-production`
- `acme-corp-order-management-development`

**Rules**:
- All lowercase
- Hyphens separate parts
- Environment name normalized (spaces → hyphens)
- Each tenant application gets separate databases per environment

---

## Status Summary

✅ **All vertical slices implemented**
✅ **Complete flow testable end-to-end**
✅ **Database provisioning working**
✅ **DDL script generation working**
✅ **Schema comparison working**
✅ **Migration approval workflow working**
✅ **Migration execution working**

**Ready for testing!** 🚀
