# Docker Images

## Redis

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `maxmemory` | 512mb | 2-4gb | Prevent OOM |
| `maxmemory-policy` | noeviction | allkeys-lru | Evict old keys |
| `appendonly` | no | yes | Data durability |
| `save` | "" | 900 1 300 10 | RDB snapshots |
| `maxclients` | 1000 | 10000 | Connection limit |

## RabbitMQ

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `vm_memory_high_watermark` | 0.4 | 0.6 | Memory limit |
| `disk_free_limit` | 1GB | 2GB | Disk space |
| `heartbeat` | 60 | 60 | Connection health |
| `channel_max` | 128 | 2047 | Max channels |
| `delegate_count` | 4 | 16 | Worker threads |

## .NET

### Alpine Image Variants

| Image | Size | Base OS | Use Case | Includes |
|-------|------|---------|----------|----------|
| `mcr.microsoft.com/dotnet/aspnet:9.0` | ~220 MB | Debian | Default runtime with ASP.NET Core | ASP.NET Core runtime, .NET runtime, glibc |
| `mcr.microsoft.com/dotnet/aspnet:9.0-alpine` | ~110 MB | Alpine Linux | Smaller runtime with ASP.NET Core | ASP.NET Core runtime, .NET runtime, musl libc |
| `mcr.microsoft.com/dotnet/sdk:9.0` | ~750 MB | Debian | Build and development | SDK, runtime, build tools, glibc |
| `mcr.microsoft.com/dotnet/sdk:9.0-alpine` | ~550 MB | Alpine Linux | Smaller build image | SDK, runtime, build tools, musl libc |
| `mcr.microsoft.com/dotnet/runtime:9.0-alpine` | ~85 MB | Alpine Linux | Minimal runtime (no ASP.NET) | .NET runtime only, musl libc |
| `mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine` | ~15 MB | Alpine Linux | Self-contained apps only | Native dependencies only |

### Summary Comparison

| Aspect | Debian-based Images | Alpine-based Images |
|--------|---------------------|---------------------|
| **Image Size** | ~220 MB (runtime) | ~110 MB (runtime) |
| **Size Reduction** | Baseline | **50% smaller** |
| **C Library** | glibc (GNU C Library) | musl libc |
| **Package Manager** | `apt` / `apt-get` | `apk` |
| **Base OS** | Debian 12 (Bookworm) | Alpine Linux 3.20 |
| **Security Updates** | Monthly | Weekly |
| **CVE Count** | Higher (more packages) | Lower (minimal packages) |
| **Attack Surface** | Larger | **Smaller** |
| **Compatibility** | Excellent (industry standard) | Good (may need adjustments) |
| **Native Libraries** | Pre-installed (many) | Minimal (install as needed) |
| **Globalization** | Built-in ICU | Requires `icu-libs` package |
| **SSL/TLS** | Built-in OpenSSL | Requires `ca-certificates` |
| **Startup Time** | Slightly slower | **Slightly faster** |
| **Memory Usage** | Higher baseline | **Lower baseline** |
| **Build Time** | Slower (larger base) | **Faster (smaller base)** |
| **Docker Pull Time** | Slower | **Faster** |
| **Production Use** | Very common | Increasingly common |
| **Debugging** | More tools available | Fewer tools (minimal) |
| **Shell** | bash | sh (ash) |
| **User Management** | `useradd` / `groupadd` | `adduser` / `addgroup` |
| **Best For** | Maximum compatibility | Size-sensitive deployments |
| **Kubernetes** | Excellent | **Excellent** |
| **Cloud Native** | Good | **Better** |
| **Container Registry Costs** | Higher (larger images) | **Lower (smaller images)** |
| **Network Transfer** | Slower | **Faster** |
| **Cold Start (Serverless)** | Slower | **Faster** |
| **Recommended For** | Legacy apps, maximum compatibility | New apps, microservices, cloud-native |

### Memory & Garbage Collection

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_GCHeapHardLimit` | 500000000 (500 MB) | 1000000000 (1 GB) | Hard limit on GC heap size to prevent OOM |
| `DOTNET_GCHeapHardLimitPercent` | - | 75 | Alternative: Use percentage of available memory |
| `DOTNET_gcServer` | true | true | Enable server GC (optimized for throughput on multi-core) |
| `DOTNET_GCHeapCount` | 1 | 2-4 | Number of GC heaps (usually = CPU cores) |
| `DOTNET_GCConserveMemory` | 0 | 0-9 | Memory conservation level (0=none, 9=max) |
| `DOTNET_GCRetainVM` | false | true | Retain virtual memory segments for reuse |
| `DOTNET_GCLOHThreshold` | 85000 | 85000 | Large object heap threshold (bytes) |

### Thread Pool

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_ThreadPool_UnfairSemaphoreSpinLimit` | 6 | 6 | Spin count before blocking on semaphore |
| `DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT` | 2 | 4 | Socket async I/O thread count |
| `DOTNET_ThreadPool_EnableWorkerTracking` | false | false | Enable thread pool worker tracking (perf overhead) |

### Diagnostics & Monitoring

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_EnableDiagnostics` | 1 | 0 | Enable diagnostic ports (debugger, profiler) |
| `DOTNET_EnableEventPipe` | 1 | 0 | Enable EventPipe for tracing |
| `DOTNET_EventPipeOutputPath` | - | - | Path for EventPipe output files |
| `DOTNET_SYSTEM_DIAGNOSTICS_METRICS_ENABLED` | true | true | Enable metrics collection |
| `DOTNET_SYSTEM_DIAGNOSTICS_DEFAULTACTIVITYIDFORMATISHIERARCHIAL` | false | false | Use hierarchical activity IDs |

### Networking & Sockets

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS` | 1 | 1 | Inline socket completion callbacks (reduces allocations) |
| `DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT` | true | true | Enable HTTP/2 support |
| `DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP3SUPPORT` | false | true | Enable HTTP/3 support (requires QUIC) |
| `DOTNET_SYSTEM_NET_SOCKETS_IPPROTECTIONLEVEL` | - | - | IP protection level for sockets |

### Globalization

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT` | false | false | Use invariant culture (smaller image, no localization) |
| `DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY` | false | false | Use only predefined cultures |

### JIT Compilation

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_TieredCompilation` | true | true | Enable tiered compilation (quick JIT → optimized JIT) |
| `DOTNET_TC_QuickJitForLoops` | true | true | Use quick JIT for loops |
| `DOTNET_ReadyToRun` | false | true | Use ReadyToRun images (pre-compiled) |
| `DOTNET_TieredPGO` | false | true | Enable Profile-Guided Optimization |

### Kestrel Web Server

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `Kestrel__Limits__MaxConcurrentConnections` | 100 | 1000 | Maximum concurrent connections |
| `Kestrel__Limits__MaxConcurrentUpgradedConnections` | 100 | 1000 | Maximum WebSocket/upgraded connections |
| `Kestrel__Limits__MaxRequestBodySize` | 30000000 (30 MB) | 10485760 (10 MB) | Maximum request body size |
| `Kestrel__Limits__KeepAliveTimeout` | 00:02:00 | 00:02:00 | Keep-alive timeout |
| `Kestrel__Limits__RequestHeadersTimeout` | 00:00:30 | 00:00:30 | Request headers timeout |
| `Kestrel__Limits__MaxRequestHeaderCount` | 100 | 100 | Maximum request headers |
| `Kestrel__Limits__MaxRequestHeadersTotalSize` | 32768 (32 KB) | 32768 (32 KB) | Total size of request headers |
| `Kestrel__Limits__MaxRequestLineSize` | 8192 (8 KB) | 8192 (8 KB) | Maximum request line size |
| `Kestrel__Limits__MaxResponseBufferSize` | 65536 (64 KB) | 65536 (64 KB) | Response buffer size |
| `Kestrel__EndpointDefaults__Protocols` | Http1AndHttp2 | Http1AndHttp2 | Supported HTTP protocols |
| `Kestrel__AddServerHeader` | true | false | Add "Server" header to responses |

### ASP.NET Core

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Development | Production | Application environment |
| `ASPNETCORE_URLS` | http://+:8080 | http://+:8080 | Listening URLs |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | false | true | Enable forwarded headers (for load balancers) |
| `ASPNETCORE_DETAILEDERRORS` | true | false | Show detailed error pages |
| `ASPNETCORE_HOSTINGSTARTUP_PREVENTHOSTINGSTARTUP` | false | true | Prevent hosting startup assemblies |

### Response Compression

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `ResponseCompression__EnableForHttps` | false | true | Enable compression for HTTPS |
| `ResponseCompression__MimeTypes` | - | application/json,text/plain,text/html | MIME types to compress |

### Logging

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `Logging__LogLevel__Default` | Debug | Information | Default log level |
| `Logging__LogLevel__Microsoft` | Information | Warning | Microsoft namespace log level |
| `Logging__LogLevel__Microsoft.AspNetCore` | Warning | Warning | ASP.NET Core log level |
| `Logging__LogLevel__System` | Information | Warning | System namespace log level |
| `Logging__Console__FormatterName` | simple | json | Console log formatter |
| `Logging__Console__FormatterOptions__SingleLine` | false | true | Single-line console logs |
| `Logging__Console__FormatterOptions__IncludeScopes` | true | false | Include log scopes |

### OpenTelemetry

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | http://seq:5341 | http://seq:5341 | OTLP exporter endpoint |
| `OTEL_SERVICE_NAME` | monolith | monolith | Service name for telemetry |
| `OTEL_TRACES_SAMPLER` | always_on | parentbased_traceidratio | Trace sampling strategy |
| `OTEL_TRACES_SAMPLER_ARG` | - | 0.1 | Sampling ratio (10%) |
| `OTEL_METRICS_EXPORTER` | otlp | otlp | Metrics exporter |
| `OTEL_LOGS_EXPORTER` | otlp | otlp | Logs exporter |

### Entity Framework Core

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `EFCore__EnableSensitiveDataLogging` | true | false | Log sensitive data (SQL parameters) |
| `EFCore__EnableDetailedErrors` | true | false | Include detailed error messages |
| `EFCore__CommandTimeout` | 30 | 30 | Database command timeout (seconds) |
| `EFCore__MaxRetryCount` | 3 | 6 | Max retry count for transient failures |
| `EFCore__MaxRetryDelay` | 00:00:30 | 00:00:30 | Max delay between retries |

### Health Checks

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `HealthChecks__Timeout` | 00:00:05 | 00:00:10 | Health check timeout |
| `HealthChecks__Period` | 00:00:15 | 00:00:30 | Health check period |
| `HealthChecks__FailureStatus` | Unhealthy | Unhealthy | Status on failure |

### Docker & Container

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_RUNNING_IN_CONTAINER` | true | true | Indicates running in container |
| `DOTNET_USE_POLLING_FILE_WATCHER` | true | false | Use polling for file changes (for volumes) |
| `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE` | true | false | Reload config on file change |

### Security

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `ASPNETCORE_HTTPS_PORT` | 8443 | 443 | HTTPS port |
| `ASPNETCORE_Kestrel__Certificates__Default__Path` | - | /app/cert.pfx | Certificate path |
| `ASPNETCORE_Kestrel__Certificates__Default__Password` | - | ${CERT_PASSWORD} | Certificate password |
| `DataProtection__ApplicationName` | Datarizen | Datarizen | Data protection application name |
| `DataProtection__KeyLifetime` | 90 | 90 | Key lifetime (days) |

### Aspire-Specific

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `DOTNET_ASPIRE_SHOW_DASHBOARD_RESOURCES` | true | false | Show resources in Aspire dashboard |
| `DOTNET_ASPIRE_CONTAINER_RUNTIME` | docker | docker | Container runtime (docker/podman) |

### Custom Application Settings

| Setting | Development | Production | Purpose |
|---------|-------------|------------|---------|
| `App__EnableSwagger` | true | false | Enable Swagger UI |
| `App__EnableCors` | true | true | Enable CORS |
| `App__CorsOrigins` | http://localhost:3000 | https://app.datarizen.com | Allowed CORS origins |
| `App__JwtSecret` | dev-secret-key | ${JWT_SECRET} | JWT signing key |
| `App__JwtExpirationMinutes` | 60 | 15 | JWT token expiration |
| `App__RefreshTokenExpirationDays` | 30 | 7 | Refresh token expiration |
| `App__MaxLoginAttempts` | 10 | 5 | Max failed login attempts |
| `App__LockoutDurationMinutes` | 5 | 30 | Account lockout duration |
