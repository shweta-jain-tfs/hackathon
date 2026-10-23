namespace Pharos.Server.Application.Settings.Attributes;

public enum DependencyType
{
    Hide,
    Disable
}

/// <summary>
/// Specifies a UI-dependency of a setting from another setting.
/// The main setting property must be declared in the same class.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DependsFromAttribute : Attribute
{
    /// <summary>
    /// Specifies the name of a propertiey/properties from which the current element depends.
    /// The property must be declared in the same class.
    /// </summary>
    public IEnumerable<string> Property { get; }

    /// <summary>
    /// Specifies the value/values of the property that activates the dependency specifies in
    /// <see cref="DependencyType"/>.
    /// </summary>
    public IEnumerable<string> Value { get; }

    /// <summary>
    /// The dependency type for this UI element.
    /// </summary>
    public DependencyType DependencyType { get; }

    // Constructor for single property and value
    public DependsFromAttribute(string property, string value, DependencyType dependencyType)
    {
        Property = new[] { property };
        Value = new[] { value };
        DependencyType = dependencyType;
    }

    // Constructor for multiple properties and values
    public DependsFromAttribute(string[] properties, string[] values, DependencyType dependencyType)
    {
        Property = properties;
        Value = values;
        DependencyType = dependencyType;
    }
}