namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Settings with this attribute have their value obfuscated in the UI and treated as a password field.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ObfuscateSettingAttribute : Attribute
{
}