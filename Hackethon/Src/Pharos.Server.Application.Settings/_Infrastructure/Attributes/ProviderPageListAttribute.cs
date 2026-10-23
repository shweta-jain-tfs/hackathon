namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Marks a PageList property as provider-managed. Entries are created from the
/// specified provider and matched to existing settings by the configured key property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ProviderPageListAttribute(string providerKey, string keyPropertyName) : Attribute
{
    public string ProviderKey { get; } = string.IsNullOrWhiteSpace(providerKey)
        ? throw new ArgumentException("A provider-managed PageList must define a provider key.", nameof(providerKey))
        : providerKey;

    public string KeyPropertyName { get; } = string.IsNullOrWhiteSpace(keyPropertyName)
        ? throw new ArgumentException("A provider-managed PageList must define a key property name.", nameof(keyPropertyName))
        : keyPropertyName;
}
