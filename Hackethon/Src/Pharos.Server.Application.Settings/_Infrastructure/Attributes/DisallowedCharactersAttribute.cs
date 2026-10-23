namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Specifies a regular expression that identifies characters which are not allowed in a string setting value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DisallowedCharactersAttribute(string pattern) : Attribute
{
    /// <summary>
    /// Gets the regular expression used to identify disallowed characters.
    /// </summary>
    public string Pattern { get; } = pattern;
}