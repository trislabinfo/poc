namespace TenantApplication.Domain.Enums;

/// <summary>Lifecycle status of a tenant application.</summary>
public enum TenantApplicationStatus
{
    Draft = 0,
    Installed = 1,
    Active = 2,
    Inactive = 3,
    Archived = 4
}
