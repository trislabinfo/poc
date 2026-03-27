using System.Net.Http.Json;
using System.Text.Json;
using AppBuilder.Application.DTOs;
using BuildingBlocks.Web.Models;

namespace AppBuilder.McpServer;

public sealed class AppBuilderApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public AppBuilderApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AppDefinitionDto> CreateApplicationDefinitionAsync(
        CreateApplicationDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/appbuilder/application-definitions",
            request,
            JsonOptions,
            cancellationToken);

        return await ReadAppDefinitionOrThrowAsync(response, cancellationToken);
    }

    public async Task<AppDefinitionDto> GetApplicationDefinitionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"api/appbuilder/application-definitions/{id}",
            cancellationToken);

        return await ReadAppDefinitionOrThrowAsync(response, cancellationToken);
    }

    public async Task<AppDefinitionDto> UpdateApplicationDefinitionAsync(
        Guid id,
        UpdateApplicationDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/appbuilder/application-definitions/{id}",
            request,
            JsonOptions,
            cancellationToken);

        return await ReadAppDefinitionOrThrowAsync(response, cancellationToken);
    }

    private static async Task<AppDefinitionDto> ReadAppDefinitionOrThrowAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
            throw new AppBuilderApiClientException(
                $"Empty response from AppBuilder.Api ({(int)response.StatusCode}).",
                response.StatusCode);

        ApiResponse<AppDefinitionDto>? apiResponse;
        try
        {
            apiResponse = JsonSerializer.Deserialize<ApiResponse<AppDefinitionDto>>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            throw new AppBuilderApiClientException(
                $"Invalid JSON response from AppBuilder.Api ({(int)response.StatusCode}).",
                response.StatusCode);
        }

        var message =
            apiResponse?.Error?.Message
            ?? apiResponse?.Error?.Code
            ?? response.ReasonPhrase
            ?? "Request to AppBuilder.Api failed.";

        if (!response.IsSuccessStatusCode || apiResponse is null || apiResponse.Success is not true || apiResponse.Data is null)
            throw new AppBuilderApiClientException(message, response.StatusCode);

        return apiResponse.Data;
    }
}

