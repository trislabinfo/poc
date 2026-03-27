namespace PlatformMetaModel.Component;

/// <summary>
/// Property definition within a component's propsSchema.
/// </summary>
public class ComponentPropertyDefinition
{
    /// <summary>Property name.</summary>
    public string? Name { get; set; }

    /// <summary>JSON type (string, number, boolean, array, object).</summary>
    public string? Type { get; set; }

    /// <summary>Whether property is required.</summary>
    public bool? Required { get; set; }

    /// <summary>Default value for the property.</summary>
    public object? DefaultValue { get; set; }

    /// <summary>Property description/documentation.</summary>
    public string? Description { get; set; }
}
