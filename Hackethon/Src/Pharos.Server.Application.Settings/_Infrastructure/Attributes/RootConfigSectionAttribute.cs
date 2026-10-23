namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// An attribute for decorating root configuration sections. Classes decorating by this attribute
/// serve as entry points for building Pharos configuration. Every other class that is reachable
/// from a root configuration section will be automatically included into the configuration.
/// </summary>
/// <remarks>
/// A settings class:
/// 1. Must be non-abstract with a public parameterless constructor.
/// 2. Must contain public read-write properties to bind (fields are not bound).
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RootConfigSectionAttribute(int sortOrder) : Attribute
{
    public int SortOrder { get; } = sortOrder;
}