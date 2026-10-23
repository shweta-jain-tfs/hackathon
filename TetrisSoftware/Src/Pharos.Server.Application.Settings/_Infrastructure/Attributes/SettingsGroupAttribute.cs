namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Classes marked by this attribute are represented in the UI as subsections on a settings page
/// (as opposed to the nested pages). By default, every config section (class) is rendered as a page in the UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingsGroupAttribute : Attribute
{
}