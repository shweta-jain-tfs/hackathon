namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// The attribute specifies a localized display name for the UI element.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field)]
public sealed class DisplayNameLocalAttribute(string resourceName) : Attribute
{
    /// <summary>
    /// The resource name (property name) for the localized display name. The resource type is passed to AddEntityConfiguration().
    /// </summary>
    public string ResourceName { get; } = resourceName;
}
