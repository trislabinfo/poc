using AppDefinition.Contracts.DTOs;
using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Kernel.Results;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using TenantApplication.Application.DTOs;

namespace AppRuntime.BFF.Services;

/// <summary>
/// Runtime API implementation that calls the Monolith's /api/runtime/* over HTTP.
/// Used when Runtime BFF runs as a separate app and Monolith hosts all server modules (Monolith topology).
/// BFF remains responsible for aggregating and optimizing communication with the runtime client.
/// </summary>
public sealed class MonolithHttpRuntimeApi : IRuntimeApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MonolithHttpRuntimeApi> _logger;
    private const string ClientName = "monolith";

    public MonolithHttpRuntimeApi(IHttpClientFactory httpClientFactory, ILogger<MonolithHttpRuntimeApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<ResolvedApplicationDto>> ResolveAsync(string tenantSlug, string appSlug, string environment, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var url = $"api/runtime/resolve?tenantSlug={Uri.EscapeDataString(tenantSlug)}&appSlug={Uri.EscapeDataString(appSlug)}&environment={Uri.EscapeDataString(environment ?? "production")}";
        var response = await client.GetAsync(url, cancellationToken);
        return await MapResponseAsync(response, () => response.Content.ReadFromJsonAsync<ResolvedApplicationDto>(cancellationToken), "Resolve");
    }

    public async Task<Result<ApplicationSnapshotDto?>> GetSnapshotAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var response = await client.GetAsync($"api/runtime/snapshot?applicationReleaseId={applicationReleaseId}", cancellationToken);
        var result = await MapResponseAsync(response, () => response.Content.ReadFromJsonAsync<ApplicationSnapshotDto>(cancellationToken), "Snapshot");
        return result.IsSuccess ? Result<ApplicationSnapshotDto?>.Success(result.Value) : Result<ApplicationSnapshotDto?>.Failure(result.Error!);
    }

    public async Task<Result<string?>> GetInitialViewHtmlAsync(Guid applicationReleaseId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var response = await client.GetAsync($"api/runtime/initial-view?applicationReleaseId={applicationReleaseId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Result<string?>.Failure(ToError(response.StatusCode, "InitialView"));
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<string?>.Success(string.IsNullOrEmpty(html) ? null : html);
    }

    public async Task<Result<string?>> GetEntityViewHtmlAsync(Guid applicationReleaseId, Guid entityId, string viewType, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var url = $"api/runtime/view?applicationReleaseId={applicationReleaseId}&entityId={entityId}&viewType={Uri.EscapeDataString(viewType)}";
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Result<string?>.Failure(ToError(response.StatusCode, "EntityView"));
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return Result<string?>.Success(string.IsNullOrEmpty(html) ? null : html);
    }

    public async Task<Result<CompatibilityCheckResultDto>> CheckCompatibilityAsync(Guid applicationReleaseId, Guid? runtimeVersionId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var url = $"api/runtime/compatibility?applicationReleaseId={applicationReleaseId}";
        if (runtimeVersionId.HasValue)
            url += $"&runtimeVersionId={runtimeVersionId.Value}";
        var response = await client.GetAsync(url, cancellationToken);
        return await MapResponseAsync(response, () => response.Content.ReadFromJsonAsync<CompatibilityCheckResultDto>(cancellationToken), "Compatibility");
    }

    public async Task<Result<DatasourceExecuteResultDto>> ExecuteDatasourceAsync(Guid applicationReleaseId, string datasourceId, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        var body = new DatasourceExecuteRequestDto(applicationReleaseId, datasourceId);
        var response = await client.PostAsJsonAsync("api/runtime/datasource/execute", body, cancellationToken);
        return await MapResponseAsync(response, () => response.Content.ReadFromJsonAsync<DatasourceExecuteResultDto>(cancellationToken), "ExecuteDatasource");
    }

    private static async Task<Result<T>> MapResponseAsync<T>(HttpResponseMessage response, Func<Task<T?>> readJson, string operation) where T : class
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await readJson();
            return Result<T>.Success(value!);
        }
        var err = ToError(response.StatusCode, operation);
        return Result<T>.Failure(err);
    }

    private static Error ToError(System.Net.HttpStatusCode statusCode, string operation)
    {
        return statusCode switch
        {
            System.Net.HttpStatusCode.NotFound => Error.NotFound($"Runtime.{operation}.NotFound", "Not found."),
            System.Net.HttpStatusCode.BadRequest => Error.Validation($"Runtime.{operation}.BadRequest", "Bad request."),
            _ => Error.Failure($"Runtime.{operation}.Error", $"Request failed: {statusCode}")
        };
    }
}
