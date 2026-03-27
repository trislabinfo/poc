namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for updating a relation definition (AppBuilder and TenantApplication).</summary>
public sealed record UpdateRelationRequest(bool CascadeDelete);
