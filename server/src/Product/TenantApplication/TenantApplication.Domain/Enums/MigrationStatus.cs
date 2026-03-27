namespace TenantApplication.Domain.Enums;

/// <summary>Status of a schema migration between releases.</summary>
public enum MigrationStatus
{
    Pending = 0,      // Created, awaiting review
    Approved = 1,     // Reviewed and approved
    Executing = 2,    // Currently running
    Completed = 3,    // Successfully executed
    Failed = 4        // Execution failed
}
