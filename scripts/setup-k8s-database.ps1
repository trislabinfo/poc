# PowerShell script to create database and run migrations in Kubernetes
# This script is idempotent - it can be run multiple times safely

param(
    [Parameter(Mandatory=$false)]
    [string]$Topology = "Microservices",
    
    [Parameter(Mandatory=$false)]
    [int]$PortForwardPort = 5432,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipPortForward
)

$ErrorActionPreference = "Stop"

Write-Host "=== Kubernetes Database Setup and Migrations ===" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check kubectl
if (-not (Get-Command kubectl -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: kubectl is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install kubectl: https://kubernetes.io/docs/tasks/tools/" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ kubectl found" -ForegroundColor Green

# Check dotnet
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: dotnet CLI is not installed or not in PATH" -ForegroundColor Red
    exit 1
}
Write-Host "✓ dotnet CLI found" -ForegroundColor Green

# Check Kubernetes connectivity
Write-Host ""
Write-Host "Checking Kubernetes connectivity..." -ForegroundColor Yellow
try {
    kubectl cluster-info --request-timeout=5s | Out-Null
    Write-Host "✓ Connected to Kubernetes cluster" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Cannot connect to Kubernetes cluster" -ForegroundColor Red
    Write-Host "Make sure kubectl is configured correctly and the cluster is accessible" -ForegroundColor Yellow
    exit 1
}

# Get database pod
Write-Host ""
Write-Host "Finding database pod..." -ForegroundColor Yellow
$dbPod = kubectl get pods -l app.kubernetes.io/component=dr-development-db -o jsonpath='{.items[0].metadata.name}' 2>$null
if (-not $dbPod) {
    Write-Host "ERROR: Database pod not found. Is the deployment running?" -ForegroundColor Red
    Write-Host "Try running: helm install datarizen . -f values-local.yaml" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Found database pod: $dbPod" -ForegroundColor Green

# Wait for pod to be ready
Write-Host ""
Write-Host "Waiting for database pod to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
while ($attempt -lt $maxAttempts) {
    $ready = kubectl get pod $dbPod -o jsonpath='{.status.conditions[?(@.type=="Ready")].status}' 2>$null
    if ($ready -eq "True") {
        Write-Host "✓ Database pod is ready" -ForegroundColor Green
        break
    }
    $attempt++
    Write-Host "  Waiting... ($attempt/$maxAttempts)" -ForegroundColor Gray
    Start-Sleep -Seconds 2
}
if ($ready -ne "True") {
    Write-Host "WARNING: Database pod may not be fully ready, but continuing..." -ForegroundColor Yellow
}

# Get database credentials from Kubernetes
Write-Host ""
Write-Host "Retrieving database credentials..." -ForegroundColor Yellow

# Get password from secret
$dbPasswordBase64 = kubectl get secret dr-development-db-secrets -o jsonpath='{.data.POSTGRES_PASSWORD}' 2>$null
if ($dbPasswordBase64 -and ($dbPasswordBase64.Trim().Length -gt 0)) {
    try {
        $dbPassword = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($dbPasswordBase64))
        if ([string]::IsNullOrWhiteSpace($dbPassword)) {
            throw "Decoded password is empty"
        }
        Write-Host "  Password retrieved from secret" -ForegroundColor Gray
    } catch {
        Write-Host "  WARNING: Failed to decode password from secret, using default" -ForegroundColor Yellow
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
        $dbPassword = "DevPassword123!"
    }
} else {
    # Fallback to default from values-local.yaml
    $dbPassword = "DevPassword123!"
    Write-Host "  Using default password (secret not found or empty)" -ForegroundColor Yellow
}

# Validate password is set
if (-not $dbPassword -or ([string]::IsNullOrWhiteSpace($dbPassword))) {
    Write-Host "ERROR: Database password is empty or null" -ForegroundColor Red
    Write-Host "  Password value: '$dbPassword'" -ForegroundColor Yellow
    exit 1
}

# Get username from configmap
$dbUser = kubectl get configmap dr-development-db-config -o jsonpath='{.data.POSTGRES_USER}' 2>$null
if (-not $dbUser -or ([string]::IsNullOrWhiteSpace($dbUser))) {
    $dbUser = "postgres"  # Default PostgreSQL user
    Write-Host "  Using default username: postgres" -ForegroundColor Yellow
}

$dbName = "dr-development"
Write-Host "✓ Database credentials retrieved" -ForegroundColor Green
Write-Host "  User: $dbUser" -ForegroundColor Gray
Write-Host "  Database: $dbName" -ForegroundColor Gray

# Create database (idempotent)
Write-Host ""
Write-Host "Creating database (if it doesn't exist)..." -ForegroundColor Yellow

# Validate all variables are set and are strings before proceeding
Write-Host "  Validating variables..." -ForegroundColor Gray
if (-not $dbPassword) {
    Write-Host "ERROR: dbPassword is null" -ForegroundColor Red
    exit 1
}
if ($dbPassword -isnot [string]) {
    $dbPassword = [string]$dbPassword
}
if ([string]::IsNullOrWhiteSpace($dbPassword)) {
    Write-Host "ERROR: dbPassword is empty or whitespace" -ForegroundColor Red
    exit 1
}

if (-not $dbUser) {
    Write-Host "ERROR: dbUser is null" -ForegroundColor Red
    exit 1
}
if ($dbUser -isnot [string]) {
    $dbUser = [string]$dbUser
}

if (-not $dbName) {
    Write-Host "ERROR: dbName is null" -ForegroundColor Red
    exit 1
}
if ($dbName -isnot [string]) {
    $dbName = [string]$dbName
}

Write-Host "  Variables validated" -ForegroundColor Gray

# Escape password for shell: replace single quotes with '\'' (end quote, escaped quote, start quote)
Write-Host "  Escaping password..." -ForegroundColor Gray
try {
    $escapedPassword = $dbPassword.Replace("'", "'\''")
    if (-not $escapedPassword) {
        Write-Host "ERROR: Escaped password is null" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "ERROR: Failed to escape password" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  Password type: $($dbPassword.GetType().FullName)" -ForegroundColor Yellow
    exit 1
}

# Build commands to execute in the pod
# Use sh -c with proper escaping to avoid stdin issues
Write-Host "  Building commands..." -ForegroundColor Gray
# Escape database name for use in SQL string literals (single quotes in SQL)
$dbNameForSql = $dbName.Replace("'", "''")
# Build CREATE DATABASE SQL - use double quotes for PostgreSQL identifier
$createDbSql = 'CREATE DATABASE "' + $dbName + '";'
# Escape any single quotes in the SQL for shell (though there shouldn't be any)
$createDbSqlEscaped = $createDbSql.Replace("'", "'\''")
# Build commands - wrap SQL in single quotes for shell to preserve double quotes inside
$checkCommand = "export PGPASSWORD='$escapedPassword' && psql -U $dbUser -tAc 'SELECT 1 FROM pg_database WHERE datname = ''$dbNameForSql'';'"
$createCommand = "export PGPASSWORD='$escapedPassword' && psql -U $dbUser -c '$createDbSqlEscaped'"

# Check if database exists
Write-Host "  Checking if database exists..." -ForegroundColor Gray
if (-not $dbPod) {
    Write-Host "ERROR: Database pod name is null" -ForegroundColor Red
    exit 1
}

# Execute command directly using sh -c (quote the command string)
$createDbOutput = kubectl exec $dbPod -- sh -c "$checkCommand" 2>&1
if ($null -eq $createDbOutput) {
    $createDbOutput = ""
}
$createDbOutputTrimmed = $createDbOutput.ToString().Trim()
$dbExists = ($LASTEXITCODE -eq 0) -and ($createDbOutputTrimmed -eq "1")

if ($dbExists) {
    Write-Host "✓ Database '$dbName' already exists" -ForegroundColor Green
} else {
    Write-Host "  Creating database '$dbName'..." -ForegroundColor Gray
    
    # Create database
    $createResult = kubectl exec $dbPod -- sh -c "$createCommand" 2>&1
    if ($null -eq $createResult) {
        $createResult = ""
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database '$dbName' created successfully" -ForegroundColor Green
    } elseif ($createResult -match "already exists") {
        Write-Host "✓ Database '$dbName' already exists" -ForegroundColor Green
    } else {
        # Check again if it exists (race condition)
        $verifyOutput = kubectl exec $dbPod -- sh -c "$checkCommand" 2>&1
        if ($null -eq $verifyOutput) {
            $verifyOutput = ""
        }
        $verifyOutputTrimmed = $verifyOutput.ToString().Trim()
        if (($LASTEXITCODE -eq 0) -and ($verifyOutputTrimmed -eq "1")) {
            Write-Host "✓ Database '$dbName' exists (verified)" -ForegroundColor Green
        } else {
            Write-Host "ERROR: Failed to create database" -ForegroundColor Red
            Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
            Write-Host "Output: $createResult" -ForegroundColor Yellow
            exit 1
        }
    }
}

# Set up port-forwarding
$portForwardJob = $null
if (-not $SkipPortForward) {
    Write-Host ""
    Write-Host "Setting up port-forward to database..." -ForegroundColor Yellow
    
    # Check if port is already in use
    $portInUse = Get-NetTCPConnection -LocalPort $PortForwardPort -ErrorAction SilentlyContinue
    if ($portInUse) {
        Write-Host "WARNING: Port $PortForwardPort is already in use" -ForegroundColor Yellow
        Write-Host "  Attempting to use existing port-forward..." -ForegroundColor Gray
        
        # Test if it's our port-forward
        try {
            $testConn = Test-NetConnection -ComputerName localhost -Port $PortForwardPort -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
            if ($testConn.TcpTestSucceeded) {
                Write-Host "✓ Port-forward appears to be active on port $PortForwardPort" -ForegroundColor Green
            } else {
                Write-Host "ERROR: Port $PortForwardPort is in use but not accessible" -ForegroundColor Red
                Write-Host "Please stop the process using port $PortForwardPort or use -SkipPortForward" -ForegroundColor Yellow
                exit 1
            }
        } catch {
            Write-Host "ERROR: Cannot verify port-forward" -ForegroundColor Red
            exit 1
        }
    } else {
        # Start port-forward in background
        Write-Host "  Starting port-forward on localhost:$PortForwardPort..." -ForegroundColor Gray
        $portForwardJob = Start-Job -ScriptBlock {
            param($pod, $port)
            kubectl port-forward $pod $port`:5432 2>&1 | Out-Null
        } -ArgumentList $dbPod, $PortForwardPort
        
        # Wait for port-forward to be ready
        $maxAttempts = 10
        $attempt = 0
        while ($attempt -lt $maxAttempts) {
            try {
                $testConn = Test-NetConnection -ComputerName localhost -Port $PortForwardPort -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
                if ($testConn.TcpTestSucceeded) {
                    Write-Host "✓ Port-forward active on localhost:$PortForwardPort" -ForegroundColor Green
                    break
                }
            } catch {
                # Port not ready yet
            }
            $attempt++
            Start-Sleep -Milliseconds 500
        }
        
        if ($attempt -eq $maxAttempts) {
            Write-Host "WARNING: Port-forward may not be ready, but continuing..." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host ""
    Write-Host "Skipping port-forward (using -SkipPortForward)" -ForegroundColor Yellow
    Write-Host "Make sure database is accessible on localhost:$PortForwardPort" -ForegroundColor Yellow
}

# Run migrations
Write-Host ""
Write-Host "Running database migrations..." -ForegroundColor Cyan
Write-Host "  Topology: $Topology" -ForegroundColor Gray
Write-Host ""

# Set connection string
$connectionString = "Host=localhost;Port=$PortForwardPort;Database=$dbName;Username=$dbUser;Password=$dbPassword"

# Set environment variable in multiple formats to ensure it's picked up
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $connectionString, "Process")
$env:ConnectionStrings__DefaultConnection = $connectionString
# Also try ASPNETCORE_ prefix (some .NET apps use this)
[Environment]::SetEnvironmentVariable("ASPNETCORE_ConnectionStrings__DefaultConnection", $connectionString, "Process")
$env:ASPNETCORE_ConnectionStrings__DefaultConnection = $connectionString

# Verify connection string is set (without showing password)
$connectionStringForDisplay = $connectionString -replace 'Password=[^;]+', 'Password=***'
Write-Host "  Connection string: $connectionStringForDisplay" -ForegroundColor Gray

# Verify environment variable is set and matches
$envValue = [Environment]::GetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Process")
if (-not $envValue -or $envValue -ne $connectionString) {
    Write-Host "ERROR: Connection string environment variable not set correctly" -ForegroundColor Red
    Write-Host "  Expected: $connectionStringForDisplay" -ForegroundColor Yellow
    Write-Host "  Got: $($envValue -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Yellow
    exit 1
}
Write-Host "  Environment variable verified" -ForegroundColor Gray

# Navigate to project root
$projectRoot = Join-Path $PSScriptRoot ".."
Push-Location $projectRoot

try {
    # Run MigrationRunner
    # The environment variable should be picked up by Host.CreateDefaultBuilder
    # But as a fallback, also try passing via command-line arguments
    Write-Host "  Executing MigrationRunner..." -ForegroundColor Gray
    Write-Host "  Using connection string with user: $dbUser" -ForegroundColor Gray
    
    # Build arguments array - connection string as environment variable should work
    # But we can also pass it as a command-line config value
    $migrationArgs = @(
        "--topology", $Topology,
        "ConnectionStrings:DefaultConnection=$connectionString"
    )
    
    $migrationOutput = & dotnet run --project server/src/MigrationRunner -- $migrationArgs 2>&1
    
    # Check exit code
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=== Migrations Completed Successfully ===" -ForegroundColor Green
        
        # Show summary if available
        if ($migrationOutput -match "already applied|pending|Applied migration") {
            Write-Host ""
            Write-Host "Migration Summary:" -ForegroundColor Cyan
            $migrationOutput | Select-String -Pattern "(already applied|pending|Applied migration|No pending)" | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host ""
        Write-Host "=== Migration Failed ===" -ForegroundColor Red
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host ""
        Write-Host "Migration output:" -ForegroundColor Yellow
        Write-Host $migrationOutput
        exit $LASTEXITCODE
    }
} catch {
    Write-Host ""
    Write-Host "=== Error Running Migrations ===" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
    
    # Clean up port-forward job
    if ($portForwardJob) {
        Write-Host ""
        Write-Host "Stopping port-forward..." -ForegroundColor Yellow
        Stop-Job -Job $portForwardJob -ErrorAction SilentlyContinue
        Remove-Job -Job $portForwardJob -ErrorAction SilentlyContinue
        
        # Also try to kill any kubectl port-forward processes for this pod
        $kubectlProcesses = Get-Process kubectl -ErrorAction SilentlyContinue | Where-Object {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine
            $cmdLine -like "*port-forward*$dbPod*"
        }
        if ($kubectlProcesses) {
            $kubectlProcesses | Stop-Process -Force -ErrorAction SilentlyContinue
        }
        
        Write-Host "✓ Port-forward stopped" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Database '$dbName' is ready with migrations applied." -ForegroundColor Green
Write-Host "You can now use the application APIs." -ForegroundColor Green
