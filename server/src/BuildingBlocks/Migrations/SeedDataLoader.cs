using System.Reflection;
using System.Text.Json;

namespace Datarizen.BuildingBlocks.Migrations;

/// <summary>
/// Loads seed data from embedded JSON files based on environment.
/// Reusable by all module migration projects (Identity, Tenant, User, Feature).
/// </summary>
public static class SeedDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Loads seed data from a module's embedded JSON file.
    /// Searches in order: {resourcePrefix}.{environment}.{fileName}, then {resourcePrefix}.Common.{fileName}.
    /// </summary>
    /// <typeparam name="T">Type to deserialize</typeparam>
    /// <param name="assembly">Assembly that contains the embedded resources (e.g. the module's Migrations assembly)</param>
    /// <param name="resourcePrefix">Full prefix for embedded resources (e.g. "Datarizen.Identity.Migrations.SeedData")</param>
    /// <param name="fileName">File name (e.g. "roles.json")</param>
    /// <param name="environment">Environment (Development, Staging, Production). If null, uses GetEnvironment().</param>
    /// <returns>Deserialized list, or empty list if resource not found</returns>
    public static List<T> Load<T>(Assembly assembly, string resourcePrefix, string fileName, string? environment = null)
    {
        environment ??= GetEnvironment();

        // Try environment-specific resource first
        var environmentResource = $"{resourcePrefix}.{environment}.{fileName}";
        var stream = assembly.GetManifestResourceStream(environmentResource);

        // Fall back to Common/
        if (stream == null)
        {
            var commonResource = $"{resourcePrefix}.Common.{fileName}";
            stream = assembly.GetManifestResourceStream(commonResource);
        }

        if (stream == null)
        {
            return new List<T>();
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
    }

    /// <summary>
    /// Gets the current environment from environment variable.
    /// Checks ASPNETCORE_ENVIRONMENT, then DOTNET_ENVIRONMENT; defaults to "Development".
    /// </summary>
    public static string GetEnvironment()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";
    }
}
