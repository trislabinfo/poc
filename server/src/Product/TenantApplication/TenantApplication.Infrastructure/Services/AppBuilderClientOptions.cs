namespace TenantApplication.Infrastructure.Services;

/// <summary>
/// Options for calling AppBuilder API when TenantApplication runs in a separate process (microservice topology).
/// When <see cref="BaseUrl"/> is set, the HTTP snapshot provider is used; otherwise the in-process provider is used.
/// </summary>
public sealed class AppBuilderClientOptions
{
    public const string SectionName = "AppBuilder";

    /// <summary>
    /// Base URL of the AppBuilder API (e.g. https://localhost:7244).
    /// When set, install will fetch platform release snapshot via HTTP instead of in-process.
    /// </summary>
    public string? BaseUrl { get; set; }
}
