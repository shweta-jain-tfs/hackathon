namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Marks a setting option as visible but not editable in the settings UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ReadOnlySettingAttribute : Attribute;
