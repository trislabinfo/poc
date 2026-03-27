#!/usr/bin/env bash
set -euo pipefail

# generate-module.sh
# -------------------
# Scaffolds a new product module under:
#   server/src/Product/{ModuleName}/
#
# It creates seven projects per module:
#   - {ModuleName}.Module
#   - {ModuleName}.Api
#   - {ModuleName}.Domain
#   - {ModuleName}.Application
#   - {ModuleName}.Infrastructure
#   - {ModuleName}.Migrations
#   - {ModuleName}.Contracts
#
# For each project it will:
#   - Create the project directory and .csproj
#   - Add basic project references to BuildingBlocks and other module projects
#   - Add the project to server/Datarizen.sln
#   - Create a minimal README.md
#
# The scaffold aligns with `docs/ai-context/05-MODULES.md`:
#   - `{ModuleName}.Module/{ModuleName}Module.cs` implements `IModule`
#   - `{ModuleName}.Api` includes a stub controller under `api/{module}`
#
# Usage:
#   ./scripts/development/generate-module.sh Tenant
#
# Notes:
#   - Run this from the repository root.
#   - After creating the script on Unix-like systems, make it executable:
#       chmod +x ./scripts/development/generate-module.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
SERVER_DIR="$ROOT_DIR/server"
SOLUTION_PATH="$SERVER_DIR/Datarizen.sln"

if [ ! -f "$SOLUTION_PATH" ]; then
  echo "Error: Solution file not found at: $SOLUTION_PATH" >&2
  exit 1
fi

if [ "$#" -ne 1 ]; then
  echo "Usage: $(basename "$0") <ModuleName>" >&2
  exit 1
fi

MODULE_NAME="$1"
MODULE_NAME_LOWER="${MODULE_NAME,,}"

if ! [[ "$MODULE_NAME" =~ ^[A-Z][A-Za-z0-9]*$ ]]; then
  echo "Error: ModuleName must start with a letter and contain only letters and digits (e.g. Tenant, Identity)." >&2
  exit 1
fi

PRODUCT_DIR="$SERVER_DIR/src/Product/$MODULE_NAME"

echo "Scaffolding module '$MODULE_NAME' under:"
echo "  $PRODUCT_DIR"
echo

mkdir -p "$PRODUCT_DIR"

create_project() {
  local kind="$1"
  local project_dir="$PRODUCT_DIR/$MODULE_NAME.$kind"
  local project_name="$MODULE_NAME.$kind"
  local csproj_path="$project_dir/$project_name.csproj"

  if [ -f "$csproj_path" ]; then
    echo "Skipping existing project: $project_name"
  else
    mkdir -p "$project_dir"

    case "$kind" in
      Api)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\$MODULE_NAME.Application\\$MODULE_NAME.Application.csproj" />
    <ProjectReference Include="..\\$MODULE_NAME.Contracts\\$MODULE_NAME.Contracts.csproj" />
  </ItemGroup>

</Project>
EOF

        mkdir -p "$project_dir/Controllers" "$project_dir/Filters"

        local controller_path="$project_dir/Controllers/${MODULE_NAME}Controller.cs"
        if [ ! -f "$controller_path" ]; then
          cat > "$controller_path" <<EOF
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ${MODULE_NAME}.Api.Controllers;

[ApiController]
[Route("api/${MODULE_NAME_LOWER}")]
public sealed class ${MODULE_NAME}Controller : ControllerBase
{
    /// <summary>
    /// Basic module health/ping endpoint.
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { module = "${MODULE_NAME}", status = "ok" });
    }

    /// <summary>
    /// Lists resources (stub).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(new[]
        {
            new { Id = Guid.NewGuid(), Name = "Example 1" },
            new { Id = Guid.NewGuid(), Name = "Example 2" }
        });
    }

    /// <summary>
    /// Gets a single resource by id (stub).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetById(Guid id)
    {
        return Ok(new { Id = id, Name = "Example 1" });
    }

    /// <summary>
    /// Creates a resource (stub).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult Create([FromBody] object request)
    {
        return StatusCode(StatusCodes.Status501NotImplemented, new { Message = "Create not implemented yet" });
    }
}
EOF
        fi
        ;;
      Domain)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
  </ItemGroup>

</Project>
EOF
        ;;
      Application)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Contracts\BuildingBlocks.Contracts.csproj" />
  </ItemGroup>

</Project>
EOF
        ;;
      Infrastructure)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Infrastructure\BuildingBlocks.Infrastructure.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Domain\\$MODULE_NAME.Domain.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Application\\$MODULE_NAME.Application.csproj" />
  </ItemGroup>

</Project>
EOF
        ;;
      Contracts)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Contracts\BuildingBlocks.Contracts.csproj" />
  </ItemGroup>

</Project>
EOF
        ;;
      Module)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\BuildingBlocks\Kernel\BuildingBlocks.Kernel.csproj" />
    <ProjectReference Include="..\..\..\BuildingBlocks\Web\BuildingBlocks.Web.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Api\\$MODULE_NAME.Api.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Domain\\$MODULE_NAME.Domain.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Application\\$MODULE_NAME.Application.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Infrastructure\\$MODULE_NAME.Infrastructure.csproj" />
    <ProjectReference Include="..\$MODULE_NAME.Contracts\\$MODULE_NAME.Contracts.csproj" />
  </ItemGroup>

</Project>
EOF

        local module_file_path="$project_dir/${MODULE_NAME}Module.cs"
        if [ ! -f "$module_file_path" ]; then
          cat > "$module_file_path" <<EOF
using BuildingBlocks.Web.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ${MODULE_NAME}.Module;

/// <summary>
/// Module composition root (startup) for ${MODULE_NAME}.
/// </summary>
public sealed class ${MODULE_NAME}Module : IModule
{
    public string ModuleName => "${MODULE_NAME}";
    public string SchemaName => "${MODULE_NAME_LOWER}";

    public string[] GetMigrationDependencies() => Array.Empty<string>();

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // TODO: Register module services (Domain → Application → Infrastructure).

        // Expose this module's controllers to the host.
        services.AddControllers()
            .AddApplicationPart(typeof(${MODULE_NAME}.Api.Controllers.${MODULE_NAME}Controller).Assembly);

        return services;
    }

    public IApplicationBuilder ConfigureMiddleware(IApplicationBuilder app)
    {
        // TODO: Add module-specific middleware if needed.
        return app;
    }
}
EOF
        fi
        ;;
      Migrations)
        cat > "$csproj_path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\$MODULE_NAME.Infrastructure\\$MODULE_NAME.Infrastructure.csproj" />
  </ItemGroup>

</Project>
EOF
        ;;
      *)
        echo "Unknown project kind: $kind" >&2
        exit 1
        ;;
    esac

    echo "  Created project: $project_name"
  fi

  # Add project to solution if not already present
  if dotnet sln "$SOLUTION_PATH" list | grep -q "^$project_name\b"; then
    echo "  Project already in solution: $project_name"
  else
    dotnet sln "$SOLUTION_PATH" add "$csproj_path"
    echo "  Added to solution: $project_name"
  fi

  # Create a minimal README for the project
  local readme_path="$project_dir/README.md"
  if [ ! -f "$readme_path" ]; then
    cat > "$readme_path" <<EOF
# $project_name

Part of the $MODULE_NAME module.

This project was scaffolded by \`scripts/development/generate-module.sh\`.

Next steps:
- Add concrete implementation code.
- Wire the module into the appropriate host(s).
EOF
  fi
}

for kind in Module Api Domain Application Infrastructure Migrations Contracts; do
  create_project "$kind"
  echo
done

echo "Module '$MODULE_NAME' scaffolded successfully."
echo "Next steps:"
echo "  - Implement domain entities and application logic for '$MODULE_NAME'."
echo "  - Update hosts to load the new module."
echo "  - Run: dotnet build server/Datarizen.sln"

