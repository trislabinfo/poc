namespace BuildingBlocks.Web.Models;

/// <summary>
/// Simple response containing only the created resource id (e.g. for Create operations).
/// </summary>
/// <param name="Id">Created entity identifier.</param>
public sealed record IdResponse(Guid Id);
