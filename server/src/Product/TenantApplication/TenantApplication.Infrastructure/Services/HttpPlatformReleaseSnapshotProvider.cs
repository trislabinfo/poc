using System.Net.Http.Json;
using System.Text.Json;
using TenantApplication.Application.Services;

// API may return PascalCase; allow both.

namespace TenantApplication.Infrastructure.Services;

/// <summary>
/// Fetches platform release snapshot from AppBuilder via HTTP (microservice topology).
/// Use when TenantApplication and AppBuilder run in separate processes.
/// Configure <see cref="AppBuilderClientOptions.BaseUrl"/> to enable.
/// </summary>
public sealed class HttpPlatformReleaseSnapshotProvider : IPlatformReleaseSnapshotProvider
{
    private readonly HttpClient _httpClient;

    public HttpPlatformReleaseSnapshotProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<PlatformReleaseSnapshotDto?> GetSnapshotAsync(Guid platformReleaseId, CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress == null)
            return null;

        try
        {
            var response = await _httpClient.GetAsync(
                $"api/appbuilder/releases/{platformReleaseId}/snapshot",
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<AppBuilderSnapshotResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            if (dto == null)
                return null;

            var entityJson = string.IsNullOrWhiteSpace(dto.EntityJson) || dto.EntityJson == "{}" ? null : dto.EntityJson;
            var navigationJson = string.IsNullOrWhiteSpace(dto.NavigationJson) || dto.NavigationJson == "[]" ? null : dto.NavigationJson;
            return new PlatformReleaseSnapshotDto(entityJson, navigationJson);
        }
        catch
        {
            return null;
        }
    }

    private sealed class AppBuilderSnapshotResponse
    {
        public string? NavigationJson { get; set; }
        public string? PageJson { get; set; }
        public string? DataSourceJson { get; set; }
        public string? EntityJson { get; set; }
    }
}
