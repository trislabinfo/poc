using System.Net.Http.Json;
using System.Text.Json;

namespace BuildingBlocks.Web.Rest;

/// <summary>
/// Shared low-level REST client helpers for frontend modules.
/// </summary>
public abstract class RestApiClientBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    protected RestApiClientBase(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    protected HttpClient HttpClient { get; }

    protected async Task<RestCallResult<TResponse>> GetAsync<TResponse>(
        string route,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.GetAsync(route, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    protected async Task<RestCallResult<TResponse>> PostAsync<TRequest, TResponse>(
        string route,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await HttpClient.PostAsJsonAsync(route, request, JsonOptions, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    private static async Task<RestCallResult<TResponse>> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return response.IsSuccessStatusCode
                ? RestCallResult<TResponse>.Success(default, statusCode)
                : RestCallResult<TResponse>.Failure("Empty response from API.", statusCode);
        }

        try
        {
            // Support both:
            // 1) API envelope responses: { Success, Data, Error, Timestamp }
            // 2) Raw JSON responses (e.g. stub controllers that return plain arrays/objects).

            if (response.IsSuccessStatusCode)
            {
                // First try raw TResponse
                var raw = JsonSerializer.Deserialize<TResponse>(payload, JsonOptions);
                if (raw is not null)
                    return RestCallResult<TResponse>.Success(raw, statusCode);
            }

            // Then try standard envelope
            var envelope = JsonSerializer.Deserialize<ApiEnvelope<TResponse>>(payload, JsonOptions);
            if (envelope is null)
                return RestCallResult<TResponse>.Failure("Invalid API response.", statusCode);

            if (response.IsSuccessStatusCode && envelope.Success)
                return RestCallResult<TResponse>.Success(envelope.Data, statusCode);

            var message = envelope.Error?.Message
                ?? envelope.Error?.Code
                ?? $"API request failed with status {statusCode}.";

            return RestCallResult<TResponse>.Failure(message, statusCode);
        }
        catch (JsonException)
        {
            if (response.IsSuccessStatusCode)
                return RestCallResult<TResponse>.Failure("Unable to parse API response.", statusCode);

            return RestCallResult<TResponse>.Failure($"API request failed with status {statusCode}.", statusCode);
        }
    }
}

