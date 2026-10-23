namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Marks a property as unique to prevent the same value being used in multiple items.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the uniqueness check should be case-insensitive.
    /// </summary>
    public bool CaseInsensitive { get; set; }

    public UniqueAttribute(bool isCaseInsensitive = false) => CaseInsensitive = isCaseInsensitive;
}
