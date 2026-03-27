# Tenant Application — Business Overview

This document describes the **Tenant Application** capability in business terms: what it does, when you use each part of it, and how the main flows work from installing an application through to releasing, deploying, and handling migrations.

**Audience:** Business stakeholders, product owners, and analysts.

---

## What Is the Tenant Application Module?

The Tenant Application module lets your organization (the **tenant**) manage **applications** in its own space. For each tenant you can:

- **Use ready-made applications** provided by the platform (install from a catalog).
- **Build your own applications** from scratch (custom apps) or by copying and adapting a platform application (fork).
- **Organize work** with **environments** (e.g. Development, Staging, Production).
- **Package changes** into **releases** and **deploy** them into environments.
- **Track and run migrations** when moving an environment from one release to another (e.g. schema or data changes).

All operations are scoped to a **tenant**. Every URL and action is under:  
`/api/tenantapplication/tenants/{tenantId}/applications` (and below).

---

## Endpoint Summary (When to Use What)

### Applications (the “apps” your tenant has)

| What you do | Endpoint | When to use it |
|-------------|----------|----------------|
| **List applications** | `GET .../applications` | See all applications available to this tenant (installed, custom, or forked). |
| **Get one application** | `GET .../applications/{tenantApplicationId}` | Open details of a specific application (e.g. for a dashboard or detail screen). |
| **Install from catalog** | `POST .../applications/install` | Add a **platform application** to the tenant so people can use it as-is (e.g. standard CRM, HR app). |
| **Create custom app** | `POST .../applications/custom` | Create a **new application** that belongs only to this tenant (built from scratch, no platform template). |
| **Fork a platform app** | `POST .../applications/fork` | Copy a **platform application** into a **tenant-owned copy** that the tenant can then change and release independently. |

---

### Environments (where an application runs: Dev, Staging, Prod)

| What you do | Endpoint | When to use it |
|-------------|----------|----------------|
| **List environments** | `GET .../applications/{tenantApplicationId}/environments` | See all environments for an application (e.g. “Development”, “Staging”, “Production”). |
| **Get one environment** | `GET .../applications/{tenantApplicationId}/environments/{environmentId}` | View a single environment’s details and current deployment. |
| **Create environment** | `POST .../applications/{tenantApplicationId}/environments` | Add a new environment (e.g. add “Staging” or a new “Test” environment). |
| **Update environment** | `PUT .../applications/{tenantApplicationId}/environments/{environmentId}` | Change environment-specific settings (e.g. configuration JSON). |
| **Delete environment** | `DELETE .../applications/{tenantApplicationId}/environments/{environmentId}` | Remove an environment when it is no longer needed. |

---

### Releases (versioned snapshots of an application)

| What you do | Endpoint | When to use it |
|-------------|----------|----------------|
| **List releases** | `GET .../applications/{tenantApplicationId}/releases` | See all releases (versions) of a custom or forked application. |
| **Get one release** | `GET .../applications/{tenantApplicationId}/releases/{releaseId}` | Inspect a specific release (version number, notes, when it was created). |
| **Create release** | `POST .../applications/{tenantApplicationId}/releases` | Package the **current** definition of a custom/forked app into a new version (e.g. 1.2.0) so it can be deployed. |

*Note: For **installed** platform apps, the “version” is managed by the platform; tenant-specific releases are used for **custom** and **forked** applications.*

---

### Deployment (putting a release into an environment)

| What you do | Endpoint | When to use it |
|-------------|----------|----------------|
| **Deploy to environment** | `POST .../applications/{tenantApplicationId}/environments/{environmentId}/deploy` | Deploy a **release** (version) into a specific **environment** (e.g. deploy 1.2.0 to Staging). |

---

### Migrations (moving an environment from one release to another)

| What you do | Endpoint | When to use it |
|-------------|----------|----------------|
| **List migrations** | `GET .../applications/{tenantApplicationId}/environments/{environmentId}/migrations` | See all migrations recorded for that environment (e.g. “from 1.0 to 1.1”, “from 1.1 to 1.2”). |
| **Get one migration** | `GET .../applications/{tenantApplicationId}/environments/{environmentId}/migrations/{migrationId}` | Check status and details of a specific migration (e.g. pending, completed, failed). |
| **Create migration** | `POST .../applications/{tenantApplicationId}/environments/{environmentId}/migrations` | Register a **migration** for that environment: from one release (optional) to a target release, optionally with a script (e.g. schema or data changes). |

*Migrations are used when upgrading an environment from one version to another and you need to track or run upgrade steps (e.g. database schema changes).*

---

## End-to-End Flows (Business View)


 Use a platform application as-is (install and use)

**Goal:** The tenant offers a standard, platform-provided application to its users without customizing it.

**What you actually install:** You install a **platform application release**. That means you choose one application from the **catalog** (AppBuilder: applications that are public and have an active release). Each catalog entry gives you an **application release ID**. You send that release ID to the Install endpoint together with a **name** and **slug** that the tenant will use for this app (e.g. “Our CRM”, “crm”). The system does **not** copy the application’s definition (entities, pages, etc.) into the tenant; it creates a **link** from the tenant to that platform release so the tenant “has” that app under the chosen name and slug.

1. **List installable applications** (from the AppBuilder/catalog API: `GET api/appbuilder/catalog/applications` or `GET api/appbuilder/applications/installable`) to see what the tenant can install. Each item includes the **application release ID** you need for Install.
2. **Install** — `POST .../applications/install` with the chosen **ApplicationReleaseId** (from the catalog), **Name**, **Slug**, and optionally **ConfigurationJson**.  
   The tenant now has that application; it appears in **List applications**.
3. **Optional:** Create **environments** (e.g. Dev, Staging, Prod) and **deploy** the same (or newer) platform release into them via the deploy endpoint if the product supports that for installed apps.
4. **List applications** / **Get application** are used anytime the tenant wants to see what they have or show details (e.g. in an admin or catalog UI).

**Outcome:** Tenant has a “standard” application linked to a platform release; updates can follow platform releases.

#### Detailed description (internals)

- **Catalog:** The catalog API returns applications that are **public** and have at least one **active** release in AppBuilder. Each record includes the application’s name, slug, description, and the **active release ID** (and version). The tenant UI uses this list to let the user pick “which app to install.”
- **Install (what happens inside):** The system checks that no other application for this tenant already uses the requested **slug** (slug must be unique per tenant). It then creates a **TenantApplication** record with: the **tenant ID**; the **platform application release ID** you sent (stored as `ApplicationReleaseId`); the **name** and **slug** you provided; **IsCustom = false**; **Status = Installed**; **InstalledAt** set to now; and optional **ConfigurationJson** if you passed it. This record is saved in the tenant application store. No copy of the platform app’s entities, pages, or navigation is made—the tenant application simply **points at** the platform release. Runtime resolution of “what to run” for this tenant app uses that release (e.g. from AppBuilder or a runtime that resolves by release ID).
- **Environments and deploy (for installed apps):** If the product supports it, the tenant can create environments for this installed app and call **Deploy** to attach a release to an environment. For an *installed* app, the release used in deploy would typically be the same platform release (or a newer one from the platform). The deploy logic validates that the environment and release belong to the same tenant application and then updates the environment’s **ApplicationReleaseId**, **ReleaseVersion**, **DeployedAt**, and marks it active.

---

### Flow 2: Build and ship your own application (custom or forked)

**Goal:** The tenant has its own application (custom or forked), creates versions (releases), and deploys them to environments, with migrations when upgrading.

#### Step 1 — Get an application to work on

- **Custom:** `POST .../applications/custom` — create a new application from scratch (name, slug, description).
- **Fork:** `POST .../applications/fork` — copy a platform application into a tenant-owned application (name, slug, source platform release).

The new application appears in **List applications** and **Get application**.

#### Step 2 — Set up environments

- **Create environments** — `POST .../applications/{tenantApplicationId}/environments` for each place you want to run the app (e.g. Development, Staging, Production).
- Use **List environments** and **Get environment** to manage and show them in the UI.
- Use **Update environment** or **Delete environment** when you need to change or remove an environment.

#### Step 3 — Create a release (version)

- When the application definition (data model, screens, etc.) is ready, **create a release** — `POST .../applications/{tenantApplicationId}/releases` with version (e.g. Major.Minor.Patch) and release notes.
- Use **List releases** and **Get release** to see existing versions and their details.

#### Step 4 — Deploy the release to an environment

- **Deploy** — `POST .../applications/{tenantApplicationId}/environments/{environmentId}/deploy` with the **release** and **version** you want that environment to run.
- After this, that environment is “on” that release (e.g. Staging is on 1.2.0).

#### Step 5 — Upgrading an environment (migrations)

When you want to move an environment from one release to another (e.g. 1.1.0 → 1.2.0) and need to run upgrade steps (e.g. schema or data changes):

- **Create migration** — `POST .../applications/{tenantApplicationId}/environments/{environmentId}/migrations` with:
  - **From release** (optional; current version),
  - **To release** (target version),
  - Optional **migration script** (e.g. schema or data change definition).
- The system records the migration (e.g. Pending, then Completed or Failed).
- **List migrations** and **Get migration** let you monitor and report on migration status for that environment.

In practice: you create the migration when you plan or run the upgrade; then you deploy the target release to the environment (or the product may tie migration execution to deploy). The migration endpoints give you visibility and audit of what was done.

#### Detailed description (internals)

- **Step 1 — Custom:** The system checks slug uniqueness for the tenant, then creates a **TenantApplication** with **TenantId**, **Name**, **Slug**, **Description**, **IsCustom = true**, **Status = Draft**. No release ID or source release is set. The record is saved. The tenant now has an empty “shell” application; definitions (entities, pages, etc.) are added later (e.g. via AppBuilder or tenant-specific definition APIs), and the first **release** will snapshot whatever is current at create-release time.

- **Step 1 — Fork:** The system checks slug uniqueness, then creates a **TenantApplication** with **TenantId**, **SourceApplicationReleaseId** (the platform release you forked from), **Name**, **Slug**, **IsCustom = true**, **Status = Draft**. No copy of the platform app’s definitions is stored in the tenant at this moment—only the reference. When you later **create a release**, the system uses a **snapshot reader** that can resolve the current definitions for this tenant application (e.g. from the platform by source release, or from a cached snapshot) and then saves that snapshot as a **tenant application release** in the tenant’s store.

- **Step 2 — Create environment:** The system loads the **TenantApplication** aggregate (with its environments collection). It calls **CreateEnvironment** on the aggregate with the requested name and environment type (e.g. Development, Staging, Production). The aggregate creates a **TenantApplicationEnvironment** (new ID, tenant application ID, name, type), adds it to the app’s collection, and updates the app’s timestamp. The app is then updated in the repository and saved. The environment is stored as a child of the tenant application; EF persists it in the tenant_application_environments table (or equivalent) with cascade from the aggregate.

- **Step 3 — Create release:** The system loads the tenant application and calls a **tenant definition snapshot reader** to get the current definitions for this application (navigation, pages, data sources, entities) as JSON. It builds a version string (e.g. Major.Minor.Patch) and creates an **ApplicationRelease** entity (shared domain model) with the tenant application ID as the “application definition id”, the version, release notes, and the snapshot JSON blobs. This release is saved in the **tenant application release** store. The tenant application’s **release info** (current release ID, major/minor/patch) is updated and the app is saved. So a “release” is an immutable snapshot of the application’s definitions at that moment; it can later be deployed to environments.

- **Step 4 — Deploy:** The system loads the **environment** and the **release** by ID. It checks that the environment belongs to the given tenant application and that the release belongs to the same tenant application. It then calls **DeployRelease** on the environment entity with the release ID, version string, and deployed-by. The environment updates its **ApplicationReleaseId**, **ReleaseVersion**, **DeployedAt**, **DeployedBy**, and **IsActive = true**, and is saved. No data is copied; the environment now “points at” that release for runtime resolution.

- **Step 5 — Migrations:** **Create migration** loads the environment to ensure it exists, then creates a **TenantApplicationMigration** record with the environment ID, optional **FromReleaseId**, **ToReleaseId**, and optional **MigrationScriptJson**, and **Status = Pending**. This record is saved. The API does not execute the migration; it only records the intended upgrade path (from/to release) and optional script. A separate process or future API could run the script and then mark the migration completed or failed. **List migrations** and **Get migration** read these records so operations can see pending, completed, or failed upgrades per environment.

---

## Flow Diagram (Install → Release → Deploy → Migrations)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  TENANT APPLICATIONS                                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. GET APPLICATIONS          →  See all apps for the tenant                │
│  2. POST install / custom /   →  Add an app (from catalog, custom, or fork) │
│     fork                                                                     │
│  3. GET APPLICATION (by id)   →  Open one app’s details                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ENVIRONMENTS (per application)                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  4. GET ENVIRONMENTS           →  List Dev / Staging / Prod (etc.)           │
│  5. POST ENVIRONMENT           →  Create an environment                      │
│  6. GET ENVIRONMENT (by id)    →  View one environment                       │
│  7. PUT / DELETE ENVIRONMENT   →  Update or remove an environment            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                        │
          ┌─────────────────────────────┴─────────────────────────────┐
          ▼                                                           ▼
┌──────────────────────────────┐                    ┌──────────────────────────────┐
│  RELEASES (custom/forked)    │                    │  DEPLOYMENT                  │
├──────────────────────────────┤                    ├──────────────────────────────┤
│                              │                    │                              │
│  8. GET RELEASES              │                    │  11. POST DEPLOY             │
│  9. GET RELEASE (by id)       │  ───────────────► │     (release + version       │
│  10. POST RELEASE             │   “Deploy this    │      into environment)        │
│      (create new version)     │    version here”  │                              │
│                              │                    │  → Environment now runs      │
│                              │                    │    that release              │
└──────────────────────────────┘                    └──────────────────────────────┘
                                                                  │
                                                                  ▼
                                                    ┌──────────────────────────────┐
                                                    │  MIGRATIONS (per environment)│
                                                    ├──────────────────────────────┤
                                                    │                              │
                                                    │  12. POST MIGRATION           │
                                                    │      (from release → to      │
                                                    │       release; optional       │
                                                    │       script)                 │
                                                    │  13. GET MIGRATIONS           │
                                                    │  14. GET MIGRATION (by id)   │
                                                    │                              │
                                                    │  → Track upgrade steps when   │
                                                    │    moving env to new release  │
                                                    └──────────────────────────────┘
```

---

## Quick Reference: Base URL and Main Paths

- **Base:** `GET/POST .../api/tenantapplication/tenants/{tenantId}/applications`
- **One app:** `.../applications/{tenantApplicationId}`
- **Environments:** `.../applications/{tenantApplicationId}/environments` and `.../environments/{environmentId}`
- **Releases:** `.../applications/{tenantApplicationId}/releases` and `.../releases/{releaseId}`
- **Deploy:** `POST .../applications/{tenantApplicationId}/environments/{environmentId}/deploy`
- **Migrations:** `.../applications/{tenantApplicationId}/environments/{environmentId}/migrations` and `.../migrations/{migrationId}`

---

## What Is Missing and What Must Be Improved

This section lists known gaps and recommended improvements so the product can be scoped and prioritized with the business.

### 1. **Validation of platform references (Install & Fork)**

**Missing:** When a tenant **installs** or **forks** an application, the API accepts a platform application release ID but does **not** check with the AppBuilder (platform) service that this ID exists and is valid.

**Impact:** In a microservice setup, invalid or outdated IDs can be stored. The tenant may then see broken references or errors when using the application.

**Improvement:** Before saving an install or fork, the Tenant Application service should call the AppBuilder service to validate the release ID (and, for install, that it is still installable). If the ID is invalid, the request should be rejected with a clear error.

---

### 2. **Who did it? — Audit and identity**

**Missing:** When creating a **release** or performing a **deploy**, the system does not record the real user or actor. “Released by” and “Deployed by” are placeholders (not taken from authentication).

**Impact:** You cannot answer “who released this version?” or “who deployed to production?” for compliance, support, or rollback decisions.

**Improvement:** Integrate with the authentication/identity system so that every “create release” and “deploy” stores the actual user (or service account) that performed the action. Expose this in get-release and get-environment responses and in audit logs.

---

### 3. **Migration lifecycle — Complete and fail**

**Missing:** You can **create** a migration and **list** or **get** it, but there is **no API** to:

- Mark a migration as **completed** (e.g. after upgrade steps ran successfully), or  
- Mark a migration as **failed** (e.g. with an error message).

The domain supports these states; the API does not expose them.

**Impact:** Migrations stay “pending” from the API’s point of view even after the upgrade has run. Operations and support cannot close the loop or report failure through the same API they use to create and list migrations.

**Improvement:** Add endpoints (or extend the existing migration resource) to:

- Mark a migration as completed (and optionally when it was executed).  
- Mark a migration as failed (with an error message).  

Optionally, define how “execute migration” (or “run upgrade”) is triggered—e.g. by a separate process that then calls these endpoints—so the business flow (create → execute → complete/fail) is clear and auditable.

---

### 4. **Environment — Rename and richer update**

**Missing:** Updating an environment is limited to **configuration** (e.g. JSON settings). There is no way to change the **name** or **type** (e.g. Development, Staging, Production) of an environment after creation.

**Impact:** Typos or renames (e.g. “Staging” → “Pre-production”) require deleting and recreating the environment, which can lose deployment history or references.

**Improvement:** Extend the update-environment capability to allow changing the display name (and, if the product allows it, the environment type) in addition to configuration, where the domain rules permit it.

---

### 5. **Application detail — Environments and releases in one call**

**Missing:** **Get application by ID** returns only the application’s own fields. It does **not** include a summary of its environments or recent releases.

**Impact:** UIs that need a single “application dashboard” (app + its environments + latest releases) must call list-environments and list-releases separately, which adds round-trips and complexity.

**Improvement:** Consider an optional query (e.g. “include=environments,releases” or a dedicated “get application summary” endpoint) that returns the application plus a compact list of environments and optionally the latest releases, so one call can drive a dashboard or overview screen.

---

### 6. **Lists at scale — Pagination and filtering**

**Missing:** **List applications**, **list releases**, and **list migrations** return all items. There is no pagination (page size, page number) and no filtering (e.g. by status, date, or name).

**Impact:** Acceptable for small numbers of applications, releases, or migrations. For larger tenants or long-lived systems, responses can become large and slow, and UIs cannot efficiently show “first page” or “only failed migrations.”

**Improvement:** Introduce optional pagination (e.g. `page`, `pageSize`) and, where useful, filters (e.g. migration status, release date range, application type) so that list endpoints scale and support common reporting and operational needs.

---

### 7. **Consistent error handling and responses**

**Missing:** Some endpoints return a **list** directly (e.g. list applications), while others return a **result** wrapper that can indicate success or a structured error. Error format and HTTP status usage are not fully consistent across all endpoints.

**Impact:** Clients and UIs must handle different response shapes and error styles, which complicates integration and user-facing error messages.

**Improvement:** Standardize on a single pattern for success and errors (e.g. consistent HTTP status codes and a shared error payload shape) and use it for all Tenant Application endpoints, including list operations.

---

### 8. **Deploy for installed (platform) applications**

**Unclear:** The documentation notes that **deploy** may or may not apply to **installed** platform applications (as opposed to custom/forked ones). The exact rules for when deploy is allowed (e.g. only for custom/forked, or also for installed) are not fully spelled out for the business.

**Improvement:** Define and document the product rule: e.g. “Deploy is only for custom and forked applications” or “Deploy can also update the release used by an installed application in an environment.” Then reflect this in the API contract (e.g. validation and error messages) and in this overview so that business and support have a single, clear understanding.

---

### Summary of priorities (suggested)

| Priority | Area | Action |
|----------|------|--------|
| **High** | Platform reference validation | Validate install/fork release IDs with AppBuilder in microservice topology. |
| **High** | Audit and identity | Pass real user/actor for release and deploy; expose in API and audit. |
| **Medium** | Migration lifecycle | Add API to mark migration completed/failed (and optionally “execute” semantics). |
| **Medium** | Environment update | Allow rename (and possibly type) in addition to configuration. |
| **Lower** | Application summary | Optional “include environments/releases” or summary endpoint for dashboards. |
| **Lower** | Scale and consistency | Pagination and filtering on lists; consistent error handling and response shape. |
| **Clarify** | Deploy scope | Document and enforce whether deploy applies to installed apps or only custom/forked. |

---

## Summary for the Business

- **Tenant Application** gives the tenant a place to own and manage applications: install standard ones, create custom ones, or fork and adapt platform apps.
- **Environments** separate where the app runs (Dev, Staging, Prod); you create, list, update, and delete them per application.
- **Releases** are versioned snapshots of custom/forked apps; you create a release when you are ready to deploy a version.
- **Deploy** attaches a release (version) to an environment so that environment runs that version.
- **Migrations** record and optionally drive the upgrade path when moving an environment from one release to another (e.g. schema or data changes), and give visibility into migration status.

Together, these endpoints support the full path: **install or create an app → set up environments → create releases → deploy to environments → manage migrations** when upgrading.
