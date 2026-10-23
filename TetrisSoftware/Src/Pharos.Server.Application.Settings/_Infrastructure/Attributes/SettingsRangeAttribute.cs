namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Specifies a valid range for a setting.
/// The value of that setting must be between the specified range (inclusive).
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingsRangeAttribute : Attribute
{
    /// <summary>
    /// The minimum allowed value.
    /// </summary>
    public object Min { get; }

    /// <summary>
    /// The maximum allowed value.
    /// </summary>
    public object Max { get; }


    /// <summary>
    /// Optional. The unit of the value.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRangeAttribute"/> class with integer range values.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="unit">Optional. The unit of the value.</param>
    public SettingsRangeAttribute(int min, int max, string unit = "")
    {
        Min = min;
        Max = max;
        Unit = unit;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRangeAttribute"/> class with double range values.
    /// </summary>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="unit">Optional. The unit of the value.</param>
    public SettingsRangeAttribute(double min, double max, string unit = "")
    {
        Min = min;
        Max = max;
        Unit = unit;
    }
 }
