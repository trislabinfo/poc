# Implementation Plan: Component Registry

## Overview

Add a **Component Registry** to the Platform Meta-Model .NET classes to fully describe a no-code application. This is currently the only documented gap in the meta-model implementation.

## Background

The [Platform Meta-Model README](docs/implementations/platform-meta-model/README.md) section 8 describes a "Component Registry" that is documented but not implemented as .NET classes:

> Registered UI components: `id`, `category` (`field` | `layout` | `data`), `version`, optional `propsSchema`.

Currently, `componentId` in [`LayoutNode.cs`](docs/implementations/platform-meta-model/application/.net/Layout/LayoutNode.cs) uses hardcoded enum values (TextInput, NumberInput, DatePicker, etc.) instead of a proper registry definition.

---

## Tasks

### Task 1: Create ComponentDefinition Class

Create a new C# class in the appropriate folder structure.

**Location:** `docs/implementations/platform-meta-model/application/.net/Component/ComponentDefinition.cs`

**Content:**
- `Id` (string) - Unique component identifier
- `Category` (enum) - `Field`, `Layout`, or `Data`
- `Version` (string) - Component version
- `PropsSchema` (optional Dictionary) - JSON schema for component properties

### Task 2: Create ComponentPropertyDefinition Class

Define individual properties supported by components.

**Location:** `docs/implementations/platform-meta-model/application/.net/Component/ComponentPropertyDefinition.cs`

**Content:**
- `Name` (string) - Property name
- `Type` (string) - JSON type (string, number, boolean, array, object)
- `Required` (boolean) - Whether property is required
- `DefaultValue` (optional object) - Default value
- `Description` (string) - Property documentation

### Task 3: Add Component Registry to CommonPropertiesDefinition

Integrate components into the application model.

**File:** [`CommonPropertiesDefinition.cs`](docs/implementations/platform-meta-model/application/.net/Common/CommonPropertiesDefinition.cs)

**Changes:**
- Add `IList<ComponentDefinition>? Components` property

### Task 4: Create JSON Schema Definition

Add the corresponding JSON schema for validation.

**Location:** `docs/implementations/platform-meta-model/application/defs/ComponentDefinition.json`

---

## Implementation Details

### Component Categories

| Category | Description | Examples |
|----------|-------------|----------|
| `Field` | Input components | TextInput, NumberInput, DatePicker, Checkbox, TextArea, Select, Autocomplete |
| `Layout` | Container components | Section, Row, Tabs, Tab, DataTable |
| `Data` | Data display components | Chart, Card, DetailView |

### Example Usage

After implementation, applications can define:

```json
{
  "components": [
    {
      "id": "TextInput",
      "category": "Field",
      "version": "1.0.0",
      "propsSchema": {
        "placeholder": { "type": "string" },
        "maxLength": { "type": "number" }
      }
    }
  ]
}
```

---

## Files to Create/Modify

| File | Action |
|------|--------|
| `Component/ComponentDefinition.cs` | Create |
| `Component/ComponentPropertyDefinition.cs` | Create |
| `Common/CommonPropertiesDefinition.cs` | Modify - add Components property |
| `application/defs/ComponentDefinition.json` | Create |

---

## Verification

After implementation, verify:
1. All existing .NET classes still compile
2. JSON schema validates component definitions
3. ApplicationDefinition can include components array
