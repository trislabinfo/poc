# Module: Identity - Migrations Layer Refactoring Plan (Seed Data from JSON)

**Status**: 🆕 New - Seed Data Refactoring  
**Last Updated**: 2025-01-15  
**Estimated Total Time**: ~4 hours  
**Related Documents**: 
- `docs/implementations/module-identity-migrations-layer-plan.md` (Original plan)
- `docs/ai-context/07-DB-MIGRATIONS.md` (Technical implementation)

---

## Overview

This plan refactors the **seed data implementation** to load data from **JSON files** organized by **environment**. This approach:

✅ Separates data from code  
✅ Makes seed data easier to maintain and review  
✅ Supports environment-specific data (Development, Staging, Production)  
✅ Enables non-developers to modify seed data  

---

## Architecture

```
Identity.Migrations/
├── Migrations/
│   ├── Schema/                          # DDL migrations (unchanged)
│   │   └── ...
│   └── Data/                            # DML migrations (refactored)
│       ├── 20250115200000_SeedRolesAndPermissions.cs
│       └── 20250115201000_SeedUsers.cs
├── SeedData/                            # ✅ NEW: JSON seed data
│   ├── Common/                          # Shared across all environments
│   │   ├── roles.json
│   │   └── permissions.json
│   ├── Development/                     # Development-only data
│   │   └── users.json
│   ├── Staging/                         # Staging-only data
│   │   └── users.json
│   └── Production/                      # Production-only data
│       └── users.json
├── Helpers/                             # ✅ NEW: Helper classes
│   └── SeedDataLoader.cs
└── README.md
```

---

## Phase 1: Create Seed Data Infrastructure (1.5 hours)

### 1.1: Create SeedDataLoader Helper (45 minutes)

**File**: `server/src/Modules/Identity/Identity.Migrations/Helpers/SeedDataLoader.cs`

<augment_code_snippet path="server/src/Modules/Identity/Identity.Migrations/Helpers/SeedDataLoader.cs" mode="EDIT">
```csharp
using System.Reflection;
using System.Text.Json;

namespace Datarizen.Identity.Migrations.Helpers;

/// <summary>
/// Loads seed data from embedded JSON files based on environment.
/// </summary>
public static class SeedDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads seed data from JSON file.
    /// Searches in order: {environment}/, Common/
    /// </summary>
    /// <typeparam name="T">Type to deserialize</typeparam>
    /// <param name="fileName">File name (e.g., "roles.json")</param>
    /// <param name="environment">Environment (Development, Staging, Production)</param>
    /// <returns>Deserialized data or empty list if file not found</returns>
    public static List<T> Load<T>(string fileName, string environment = "Development")
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePrefix = "Datarizen.Identity.Migrations.SeedData";

        // Try environment-specific file first
        var environmentResource = $"{resourcePrefix}.{environment}.{fileName}";
        var stream = assembly.GetManifestResourceStream(environmentResource);

        // Fall back to Common/ if not found
        if (stream == null)
        {
            var commonResource = $"{resourcePrefix}.Common.{fileName}";
            stream = assembly.GetManifestResourceStream(commonResource);
        }

        if (stream == null)
        {
            // File not found - return empty list (allows optional seed data)
            return new List<T>();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
    }

    /// <summary>
    /// Gets the current environment from environment variable.
    /// Defaults to "Development" if not set.
    /// </summary>
    public static string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }
}
