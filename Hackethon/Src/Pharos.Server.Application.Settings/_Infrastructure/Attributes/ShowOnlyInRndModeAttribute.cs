namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// When applied to a settings property, hides it from the UI when RnD mode is not enabled.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ShowOnlyInRndModeAttribute : Attribute;
