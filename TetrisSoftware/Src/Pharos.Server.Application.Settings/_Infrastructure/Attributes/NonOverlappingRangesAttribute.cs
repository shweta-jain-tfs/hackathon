namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Defines two integer ranges that must not overlap when the rule is enabled.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class NonOverlappingRangesAttribute : Attribute
{
    public string FirstStartProperty { get; }
    public string FirstLengthProperty { get; }
    public string SecondStartProperty { get; }
    public string SecondLengthProperty { get; }
    public IEnumerable<string> EnabledWhenProperties { get; }
    public IEnumerable<string> EnabledWhenValues { get; }

    public NonOverlappingRangesAttribute(
        string firstStartProperty,
        string firstLengthProperty,
        string secondStartProperty,
        string secondLengthProperty)
        : this(firstStartProperty, firstLengthProperty, secondStartProperty, secondLengthProperty, [], [])
    {
    }

    public NonOverlappingRangesAttribute(
        string firstStartProperty,
        string firstLengthProperty,
        string secondStartProperty,
        string secondLengthProperty,
        string[] enabledWhenProperties,
        string[] enabledWhenValues)
    {
        FirstStartProperty = firstStartProperty;
        FirstLengthProperty = firstLengthProperty;
        SecondStartProperty = secondStartProperty;
        SecondLengthProperty = secondLengthProperty;
        EnabledWhenProperties = enabledWhenProperties;
        EnabledWhenValues = enabledWhenValues;
    }
}
