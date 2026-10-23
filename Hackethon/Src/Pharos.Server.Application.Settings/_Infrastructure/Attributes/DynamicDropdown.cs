namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Properties marked by this attribute are represented in the UI as dropdowns with a specific PageList entries as items
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DynamicDropdown : Attribute
{
    /// <summary>
    /// The type of the provider class that implements IDropdownValueProvider.
    /// Used for provider-based dynamic dropdowns.
    /// </summary>
    public Type? ProviderType { get; }

    /// <summary>
    /// Specifies the name of a property which will be the dropdown options.
    /// The property must be declared in the same class.
    /// </summary>
    public string? DynamicDropdownOrigin { get; }

    /// <summary>
    /// Specifies the name of a property which will be the dropdown options value in the DB.
    /// The target property must be marked with `Unique` attribute
    /// </summary>
    public string? TargetValue { get; }

    /// <summary>
    /// Constructor for provider-based dynamic dropdown.
    /// </summary>
    /// <param name="providerType">Type that implements IDropdownValueProvider</param>
    /// <exception cref="ArgumentException">Thrown when providerType doesn't implement IDropdownValueProvider</exception>
    public DynamicDropdown(Type providerType)
    {
        if (!typeof(IDropdownValueProvider).IsAssignableFrom(providerType))
        {
            throw new ArgumentException(
                $"Provider type '{providerType.FullName}' must implement IDropdownValueProvider interface.",
                nameof(providerType));
        }

        ProviderType = providerType;
    }

    /// <summary>
    /// Constructor for PageList-based dynamic dropdown.
    /// </summary>
    /// <param name="dynamicDropdownOrigin">Name of property containing dropdown options</param>
    /// <param name="targetValue">Name of property containing dropdown value target</param>
    public DynamicDropdown(string dynamicDropdownOrigin, string targetValue)
    {
        DynamicDropdownOrigin = dynamicDropdownOrigin;
        TargetValue = targetValue;
    }
}