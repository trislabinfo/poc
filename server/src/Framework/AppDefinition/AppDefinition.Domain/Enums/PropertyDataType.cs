namespace AppDefinition.Domain.Enums;

/// <summary>Data type of an entity property (shared by AppBuilder and TenantApplication).</summary>
public enum PropertyDataType
{
    String = 0,
    Number = 1,
    Boolean = 2,
    DateTime = 3,
    Date = 4,
    Time = 5,
    Json = 6
}
