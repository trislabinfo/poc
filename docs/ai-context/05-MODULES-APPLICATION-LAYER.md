# Datarizen AI Context - Module Application Layer Implementation Guide

## Overview

This guide provides step-by-step instructions for implementing the **Application Layer** of a new module. The Application Layer orchestrates business logic using the CQRS pattern with MediatR, handles validation, and provides DTOs for external communication.

**Target Audience**: AI coding assistants helping developers create new modules.

**Prerequisites**:
- Domain Layer completed (entities, value objects, repositories)
- Module name decided (e.g., "Identity", "Tenant", "Product")
- Use cases documented (commands and queries)

**Time Estimate**: 15-20 hours for a typical module with 10-15 use cases

---

## Application Layer Principles

### Core Rules

1. **CQRS Pattern**
   - ✅ Commands for write operations (Create, Update, Delete)
   - ✅ Queries for read operations (GetById, GetAll, Search)
   - ✅ Separate models for commands and queries
   - ✅ Use MediatR for request/response handling

2. **Result Pattern**
   - ✅ All handlers return `Result<T>` or `Result`
   - ❌ Never throw exceptions for business rule violations
   - ✅ Use `Error.Validation()`, `Error.NotFound()`, `Error.Conflict()`, `Error.Failure()`

3. **Validation**
   - ✅ Use FluentValidation for input validation
   - ✅ Validators run in `ValidationBehavior` (before handler)
   - ✅ Validate commands/queries, not domain entities

4. **No Infrastructure Dependencies**
   - ✅ Depends on Domain only
   - ❌ No EF Core, no database concerns
   - ✅ Use repository interfaces from Domain
   - ✅ Use abstractions for external services

5. **Custom Mappers**
   - ✅ Manual mapping (no AutoMapper)
   - ✅ Static mapper classes with explicit methods
   - ✅ Map domain entities to DTOs

6. **Specifications**
   - ✅ Use Ardalis.Specification for complex queries
   - ✅ Reusable query logic
   - ✅ Type-safe query building

---

## Project Structure

```
{ModuleName}.Application/
├── Commands/
│   ├── Create{EntityName}/
│   │   ├── Create{EntityName}Command.cs
│   │   ├── Create{EntityName}CommandHandler.cs
│   │   └── Create{EntityName}CommandValidator.cs
│   ├── Update{EntityName}/
│   │   ├── Update{EntityName}Command.cs
│   │   ├── Update{EntityName}CommandHandler.cs
│   │   └── Update{EntityName}CommandValidator.cs
│   └── Delete{EntityName}/
│       ├── Delete{EntityName}Command.cs
│       └── Delete{EntityName}CommandHandler.cs
├── Queries/
│   ├── Get{EntityName}ById/
│   │   ├── Get{EntityName}ByIdQuery.cs
│   │   └── Get{EntityName}ByIdQueryHandler.cs
│   ├── GetAll{EntityName}s/
│   │   ├── GetAll{EntityName}sQuery.cs
│   │   └── GetAll{EntityName}sQueryHandler.cs
│   └── Search{EntityName}s/
│       ├── Search{EntityName}sQuery.cs
│       └── Search{EntityName}sQueryHandler.cs
├── DTOs/
│   ├── {EntityName}Dto.cs
│   ├── {EntityName}DetailDto.cs
│   └── {EntityName}SummaryDto.cs
├── Mappers/
│   ├── {EntityName}Mapper.cs
│   └── {AnotherEntity}Mapper.cs
├── Specifications/
│   ├── {EntityName}ByIdSpecification.cs
│   ├── {EntityName}ByNameSpecification.cs
│   └── {EntityName}SearchSpecification.cs
├── Services/
│   ├── I{ServiceName}Service.cs
│   └── {ServiceName}Service.cs (optional, in Infrastructure)
├── Extensions/
│   └── ResultExtensions.cs
└── DependencyInjection.cs
```

---

## Step-by-Step Implementation

### Step 1: Project Setup (30 minutes)

#### 1.1: Create Project

**Location**: `server/src/Modules/{ModuleName}/{ModuleName}.Application/`

**Command**:
```bash
dotnet new classlib -n Datarizen.{ModuleName}.Application -f net10.0
```

#### 1.2: Add Project References

<augment_code_snippet path="server/src/Modules/{ModuleName}/{ModuleName}.Application/Datarizen.{ModuleName}.Application.csproj" mode="EDIT">
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Module Dependencies -->
    <ProjectReference Include="..\{ModuleName}.Domain\Datarizen.{ModuleName}.Domain.csproj" />
    
    <!-- BuildingBlocks Dependencies -->
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\Datarizen.BuildingBlocks.Kernel.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- MediatR for CQRS -->
    <PackageReference Include="MediatR" Version="13.0.0" />
    
    <!-- FluentValidation for input validation -->
    <PackageReference Include="FluentValidation" Version="11.9.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
    
    <!-- Ardalis.Specification for query specifications -->
    <PackageReference Include="Ardalis.Specification" Version="8.0.0" />
  </ItemGroup>
</Project>