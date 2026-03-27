# Datarizen Scaffold Plan

This document tracks the implementation of the initial project scaffold.

## Overview

The scaffold plan establishes the foundational structure for Datarizen, including:

- Solution structure and build configuration
- Building blocks (shared infrastructure)
- Product modules (Tenant, Identity, User, Feature)
- Host projects (Monolith, MultiApp hosts)
- API Gateway
- Aspire orchestration
- Database migrations
- Documentation

---

## Phase 1: Solution Structure (Completed)

**Status**: ✅ Complete

- [x] Create `server/Datarizen.sln`
- [x] Create `Directory.Build.props`
- [x] Create `Directory.Packages.props`
- [x] Create `.editorconfig`
- [x] Create folder structure under `server/src/`

---

## Phase 2: Building Blocks (Completed)

**Status**: ✅ Complete

- [x] Create `BuildingBlocks.Kernel` project
- [x] Create `BuildingBlocks.Infrastructure` project
- [x] Create `BuildingBlocks.Contracts` project
- [x] Add to solution

---

## Phase 3: Service Defaults & AppHost (Completed)

**Status**: ✅ Complete

- [x] Create `ServiceDefaults` project
- [x] Create `AppHost` project with Aspire orchestration
- [x] Configure topology switching (Monolith, MultiApp, Microservices)
- [x] Add PostgreSQL, Redis, RabbitMQ resources

---

## Phase 4: Host Projects (Completed)

**Status**: ✅ Complete

- [x] Create `MonolithHost` project
- [x] Create `MultiAppControlPanelHost` project
- [x] Create `MultiAppRuntimeHost` project
- [x] Create `MultiAppAppBuilderHost` project
- [x] Create `ApiGateway` project

---

## Phase 5: Module Scaffolding Script (Completed)

**Status**: ✅ Complete

- [x] Create `scripts/development/generate-module.sh`
- [x] Script generates 7 projects per module
- [x] Script adds projects to solution
- [x] Script creates README files

---

## Phase 6: Generate Initial Modules (Completed)

**Status**: ✅ Complete

- [x] Generate Tenant module (7 projects)
- [x] Generate Identity module (7 projects)
- [x] Generate User module (7 projects)
- [x] Generate Feature module (7 projects)

---

## Phase 7: BuildingBlocks.Web & IModule Interface (Completed)

**Status**: ✅ Complete

**Objective**: Create standardized module registration infrastructure.

### Completed Tasks:

1. **Created BuildingBlocks.Web Project**
   - [x] Created `server/src/BuildingBlocks/Web/BuildingBlocks.Web.csproj`
   - [x] Added framework reference to `Microsoft.AspNetCore.App`
   - [x] Added to solution

2. **Created IModule Interface**
   - [x] Created `server/src/BuildingBlocks/Web/Modules/IModule.cs`
   - [x] Defined contract with `ModuleName`, `SchemaName`, `GetMigrationDependencies()`
   - [x] Defined `RegisterServices()` and `ConfigureMiddleware()` methods
   - [x] Added XML documentation

3. **Created ModuleExtensions**
   - [x] Created `server/src/BuildingBlocks/Web/Extensions/ModuleExtensions.cs`
   - [x] Implemented `AddModule<TModule>()` extension method
   - [x] Implemented `UseModule<TModule>()` extension method
   - [x] Added XML documentation

4. **Built and Verified**
   - [x] Built `BuildingBlocks.Web` successfully
   - [x] No compilation errors

5. **Created Documentation**
   - [x] Created `server/src/BuildingBlocks/Web/Modules/README.md`
   - [x] Documented `IModule` interface usage
   - [x] Added implementation examples

**Deliverable**: ✅ Working `IModule` infrastructure in `BuildingBlocks.Web`.

---

## Phase 8: Implement IModule in Modules (Completed)

**Status**: ✅ Complete

**Objective**: Refactor all 4 modules to implement `IModule` interface.

### Completed Tasks:

**For Each Module (Tenant, Identity, User, Feature):**

1. **Updated Module Project References**
   - [x] Added reference to `BuildingBlocks.Web` in all module projects
   - [x] Verified references to `{ModuleName}.Api` exist

2. **Created {ModuleName}Module.cs**
   - [x] Tenant module: `SchemaName => "tenant"`, no dependencies
   - [x] Identity module: `SchemaName => "identity"`, depends on `["Tenant"]`
   - [x] User module: `SchemaName => "user"`, depends on `["Tenant", "Identity"]`
   - [x] Feature module: `SchemaName => "feature"`, depends on `["Tenant"]`

3. **Removed Old Extension Methods**
   - [x] Deleted old `ModuleServiceCollectionExtensions.cs` files
   - [x] Removed legacy `Add{ModuleName}Module()` methods

4. **Built All Modules**
   - [x] All modules build successfully
   - [x] No compilation errors

**Deliverable**: ✅ All 4 modules implement `IModule` interface with correct schema names.

---

## Phase 9: Update Host Projects (Completed)

**Status**: ✅ Complete

**Objective**: Update all host projects to use the new `IModule` pattern.

### Completed Tasks:

1. **Updated MonolithHost**
   - [x] Added reference to `BuildingBlocks.Web`
   - [x] Updated `Program.cs` to use `AddModule<T>()` and `UseModule<T>()`
   - [x] Builds successfully

2. **Updated MultiAppControlPanelHost**
   - [x] Added reference to `BuildingBlocks.Web`
   - [x] Updated `Program.cs` with conditional module loading
   - [x] Builds successfully

3. **Updated MultiAppRuntimeHost**
   - [x] Added reference to `BuildingBlocks.Web`
   - [x] Updated `Program.cs` with conditional module loading
   - [x] Builds successfully

4. **Updated MultiAppAppBuilderHost**
   - [x] Added reference to `BuildingBlocks.Web`
   - [x] Updated `Program.cs` with conditional module loading
   - [x] Builds successfully

5. **Verified Solution Builds**
   - [x] Entire solution builds without errors

**Deliverable**: ✅ All 4 hosts use the new `IModule` pattern.

---

## Phase 10: Create Stub API Controllers (Completed)

**Status**: ✅ Complete

**Objective**: Create working API controllers that return fake data or HTTP 501.

### Completed Tasks:

**For Each Module:**

1. **Tenant Module - TenantController**
   - [x] Implemented `GET /api/tenant` - Returns fake tenant list (200 OK)
   - [x] Implemented `GET /api/tenant/{id}` - Returns fake tenant (200 OK)
   - [x] Implemented `POST /api/tenant` - Returns 201 Created with new ID
   - [x] Implemented `PUT /api/tenant/{id}` - Returns 501 Not Implemented
   - [x] Implemented `DELETE /api/tenant/{id}` - Returns 501 Not Implemented
   - [x] Added XML documentation
   - [x] Added `[ProducesResponseType]` attributes

2. **Identity Module - IdentityController**
   - [x] Implemented `POST /api/identity/login` - Returns fake JWT token (200 OK)
   - [x] Implemented `POST /api/identity/register` - Returns 501 Not Implemented
   - [x] Implemented `POST /api/identity/refresh` - Returns 501 Not Implemented
   - [x] Added XML documentation
   - [x] Added `[ProducesResponseType]` attributes

3. **User Module - UserController**
   - [x] Implemented `GET /api/user` - Returns fake user list (200 OK)
   - [x] Implemented `GET /api/user/{id}` - Returns fake user (200 OK)
   - [x] Implemented `POST /api/user` - Returns 501 Not Implemented
   - [x] Added XML documentation
   - [x] Added `[ProducesResponseType]` attributes

4. **Feature Module - FeatureController**
   - [x] Implemented `GET /api/feature` - Returns fake feature flags (200 OK)
   - [x] Implemented `GET /api/feature/{id}` - Returns fake feature (200 OK)
   - [x] Implemented `POST /api/feature/{id}/toggle` - Returns 501 Not Implemented
   - [x] Added XML documentation
   - [x] Added `[ProducesResponseType]` attributes

5. **Built All API Projects**
   - [x] Built Tenant.Api successfully
   - [x] Built Identity.Api successfully
   - [x] Built User.Api successfully
   - [x] Built Feature.Api successfully

**Deliverable**: ✅ All 4 modules have working stub controllers with proper HTTP responses.

---

## Phase 11: Test Running Application (Not Started)

**Status**: ⏳ Not Started

**Objective**: Verify the application runs and API endpoints are accessible.

**Note**: This phase will be tested manually by the team. Both Monolith and MultiApp topologies must be verified.

### Tasks:

1. **Start via Aspire**
   - [ ] Run: `dotnet run --project server/src/AppHost`
   - [ ] Verify Aspire dashboard opens
   - [ ] Verify all services show green status

2. **Test Monolith Topology**
   - [ ] Open MonolithHost Swagger UI
   - [ ] Verify all module endpoints appear
   - [ ] Test Tenant endpoints (GET 200, POST 201, PUT/DELETE 501)
   - [ ] Test Identity endpoints (POST login 200, register/refresh 501)
   - [ ] Test User endpoints (GET 200, POST 501)
   - [ ] Test Feature endpoints (GET 200, POST toggle 501)

3. **Test MultiApp Topology**
   - [ ] Switch to `DistributedApp` topology in `appsettings.json`
   - [ ] Verify ControlPanel host starts (Tenant + Identity modules)
   - [ ] Verify Runtime host starts (User module)
   - [ ] Verify AppBuilder host starts (Feature module)
   - [ ] Test endpoints via each host's Swagger UI
   - [ ] Verify module isolation (each host only has its assigned modules)

4. **Document Results**
   - [ ] Create `docs/testing/manual-test-results.md`
   - [ ] Document working endpoints
   - [ ] Document 501 endpoints
   - [ ] Add screenshots (optional)

**Deliverable**: Running application with verified API endpoints in both topologies.

---

## Phase 12: Update Documentation (Not Started)

**Status**: ⏳ Not Started

**Objective**: Document the module registration pattern and API layer.

### Tasks:

1. **Update Module Documentation**
   - [ ] Update `docs/ai-context/05-MODULES.md`
   - [ ] Add `IModule` interface section
   - [ ] Add module implementation examples
   - [ ] Update host registration examples
   - [ ] Document module dependency resolution
   - [ ] Explain why `IModule` is in `BuildingBlocks.Web`

2. **Update API Documentation**
   - [ ] Update `docs/architecture/api-layer.md`
   - [ ] Document stub controller pattern
   - [ ] Document fake data responses
   - [ ] Document HTTP 501 usage

3. **Create Quick Start Guide**
   - [ ] Create `docs/getting-started/quick-start.md`
   - [ ] Document how to run the application
   - [ ] Document how to access Swagger UI
   - [ ] Document how to test endpoints
   - [ ] Add troubleshooting section

4. **Update Root README**
   - [ ] Update `README.md`
   - [ ] Add "Running the Application" section
   - [ ] Add links to documentation

**Deliverable**: Complete documentation for module registration and API layer.

---

## Phase 13: Update Module Generation Script (Not Started)

**Status**: ⏳ Not Started

**Objective**: Update `generate-module.sh` to generate modules with `IModule` implementation.

### Tasks:

1. **Update Script Templates**
   - [ ] Update `{ModuleName}Module.cs` template to implement `IModule`
   - [ ] Add `BuildingBlocks.Web` reference to `{ModuleName}.Module.csproj` template
   - [ ] Update `{ModuleName}Controller.cs` template with stub endpoints
   - [ ] Add `[ApiController]` and `[Route]` attributes to controller template
   - [ ] Add XML documentation to generated files

2. **Update Script Documentation**
   - [ ] Update script header comments
   - [ ] Document `IModule` generation
   - [ ] Add examples of generated files

3. **Test Script**
   - [ ] Generate test module
   - [ ] Verify `IModule` implementation
   - [ ] Verify stub controller generation
   - [ ] Build test module
   - [ ] Delete test module

**Deliverable**: Updated script that generates modules with `IModule` and stub controllers.

---

## Phase 14: Migration Runner Integration (Not Started)

**Status**: ⏳ Not Started

**Objective**: Update MigrationRunner to use `IModule` for dependency resolution.

### Tasks:

1. **Update MigrationRunner**
   - [ ] Add reference to `BuildingBlocks.Web`
   - [ ] Update `MigrationOrchestrator.cs` to use `IModule.GetMigrationDependencies()`
   - [ ] Remove hardcoded dependency configuration
   - [ ] Test migration dependency resolution

2. **Test Migrations**
   - [ ] Run: `dotnet run --project server/src/MigrationRunner`
   - [ ] Verify correct execution order
   - [ ] Verify all modules migrate successfully

**Deliverable**: MigrationRunner uses `IModule` for dependency resolution.

---

## Success Criteria

- ✅ Solution builds without errors
- ✅ All 4 modules implement `IModule` interface
- ✅ All 4 hosts use `AddModule<T>()` pattern
- ✅ All 4 modules have working stub controllers
- ⏳ Application runs via Aspire (pending manual testing)
- ⏳ Swagger UI shows all endpoints (pending manual testing)
- ⏳ GET endpoints return fake data (pending manual testing)
- ⏳ Unimplemented endpoints return 501 (pending manual testing)
- ⏳ Documentation is complete and accurate (pending)
- ⏳ Module generation script updated (pending)

---

## Next Steps

After completing the scaffold:

1. **Implement Domain Layer**
   - Create entities, value objects, domain events
   - Add domain logic and invariants

2. **Implement Application Layer**
   - Create MediatR handlers
   - Add DTOs and validators
   - Implement use cases

3. **Implement Infrastructure Layer**
   - Create DbContext and repositories
   - Add database migrations
   - Implement external integrations

4. **Replace Stub Controllers**
   - Update controllers to use MediatR
   - Remove fake data
   - Add proper error handling

5. **Add Authentication/Authorization**
   - Implement JWT authentication
   - Add role-based authorization
   - Secure endpoints

6. **Write Tests**
   - Unit tests
   - Integration tests
   - E2E tests






