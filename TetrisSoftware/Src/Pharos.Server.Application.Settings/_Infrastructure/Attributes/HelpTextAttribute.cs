namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// This attribute specifies a help text for a label
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field)]
public sealed class HelpTextAttribute(Type resourceType, string resourceName) : Attribute
{
    /// <summary>
    /// Gets or sets the resource name (property name) to use as the key for lookups on the resource type.
    /// </summary>
    /// <value>
    /// Use this property only in conjunction with <see cref="ResourceType" />.
    /// They are used together to retrieve the localized display name for the element.
    /// </value>
    public string ResourceName { get; } = resourceName;

    /// <summary>
    /// Gets or sets the resource type to use for display name lookups. The type must have
    /// a ResourceManager property.
    /// </summary>
    /// <value>
    /// Use this property only in conjunction with <see cref="ResourceName" />.
    /// They are used together to retrieve the localized display name for the element.
    /// </value>
    public Type ResourceType { get; init; } = resourceType;
}
