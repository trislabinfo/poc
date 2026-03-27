namespace AppDefinition.Domain.Enums;

/// <summary>Type of relationship between entities (shared by AppBuilder and TenantApplication).</summary>
public enum RelationType
{
    OneToMany = 0,
    ManyToOne = 1,
    ManyToMany = 2
}
