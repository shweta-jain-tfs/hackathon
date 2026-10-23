namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Excludes a setting, settings property subtree, or settings class subtree from settings import and export.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class ExcludeFromImportExportAttribute : Attribute;
