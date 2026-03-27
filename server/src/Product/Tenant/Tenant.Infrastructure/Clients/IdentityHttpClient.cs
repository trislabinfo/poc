using BuildingBlocks.Kernel.Results;
using Identity.Contracts;
using Identity.Contracts.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tenant.Infrastructure.Clients;

/// <summary>
/// HTTP client implementation of IIdentityApplicationService.
/// Used when Tenant and Identity run in separate processes (DistributedApp / Microservices).
/// </summary>
public sealed class IdentityHttpClient : IIdentityApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;

    public IdentityHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var body = new CreateTenantUserApiRequest(
            request.TenantId,
            null,
            request.Email,
            request.DisplayName,
            request.Password,
            request.IsTenantOwner);
        var response = await _httpClient.PostAsJsonAsync("/api/identity/create-tenant-user", body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            var code = "Identity.CreateUser";
            return response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? Result<Guid>.Failure(Error.Validation(code, error))
                : Result<Guid>.Failure(Error.Failure(code, error));
        }

        var result = await response.Content.ReadFromJsonAsync<CreateTenantUserApiResponse>(cancellationToken);
        return result?.UserId != null
            ? Result<Guid>.Success(result.UserId.Value)
            : Result<Guid>.Failure(Error.Failure("Identity.CreateUser", "Invalid response from Identity service."));
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/identity/users/{userId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, cancellationToken);
            return Result.Failure(Error.Failure("Identity.DeleteUser", error));
        }

        return Result.Success();
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                var errorEnvelope = JsonSerializer.Deserialize<ApiErrorEnvelope>(body, JsonOptions);
                if (!string.IsNullOrWhiteSpace(errorEnvelope?.Error?.Message))
                    return errorEnvelope.Error.Message;
            }
        }
        catch
        {
            // Fall through to ReasonPhrase
        }

        return response.ReasonPhrase ?? "Unknown error";
    }

    private sealed record CreateTenantUserApiRequest(
        Guid TenantId,
        string? TenantName,
        string Email,
        string DisplayName,
        string? Password,
        bool IsTenantOwner);

    private sealed record CreateTenantUserApiResponse(Guid? UserId);

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
