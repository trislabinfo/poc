using BuildingBlocks.Kernel.Results;
using System.Net.Http.Json;
using System.Text.Json;
using Tenant.Contracts;
using Tenant.Contracts.Services;

namespace TenantApplication.Infrastructure.Clients;

/// <summary>
/// HTTP client implementation of ITenantResolverService.
/// Used when TenantApplication and Tenant run in separate processes (Microservices).
/// </summary>
public sealed class TenantHttpClient : ITenantResolverService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;

    public TenantHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<TenantInfoDto?>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/tenant/by-slug/{Uri.EscapeDataString(slug)}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result<TenantInfoDto?>.Success(null);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return Result<TenantInfoDto?>.Failure(Error.Failure("Tenant.Resolve", error));
        }

        var dto = await response.Content.ReadFromJsonAsync<TenantInfoDto>(JsonOptions, cancellationToken);
        return Result<TenantInfoDto?>.Success(dto);
    }

    public async Task<Result<TenantInfoDto?>> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/tenant/{tenantId}/info", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return Result<TenantInfoDto?>.Success(null);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return Result<TenantInfoDto?>.Failure(Error.Failure("Tenant.Resolve", error));
        }

        var dto = await response.Content.ReadFromJsonAsync<TenantInfoDto>(JsonOptions, cancellationToken);
        return Result<TenantInfoDto?>.Success(dto);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                var envelope = JsonSerializer.Deserialize<ApiErrorEnvelope>(body, JsonOptions);
                if (!string.IsNullOrWhiteSpace(envelope?.Error?.Message))
                    return envelope.Error.Message;
            }
        }
        catch
        {
            // Fall through
        }

        return response.ReasonPhrase ?? "Unknown error";
    }

    private sealed class ApiErrorEnvelope
    {
        public ApiErrorPart? Error { get; set; }
    }

    private sealed class ApiErrorPart
    {
        public string? Message { get; set; }
        public string? Code { get; set; }
    }
}
