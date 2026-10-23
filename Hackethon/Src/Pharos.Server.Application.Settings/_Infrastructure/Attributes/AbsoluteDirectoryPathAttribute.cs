namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Marks a string settings property as requiring a valid absolute directory path.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AbsoluteDirectoryPathAttribute : Attribute
{
}