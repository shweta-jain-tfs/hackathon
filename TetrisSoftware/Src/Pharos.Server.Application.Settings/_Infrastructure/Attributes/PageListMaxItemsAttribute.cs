namespace Pharos.Server.Application.Settings.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PageListMaxItemsAttribute(int maxItems) : Attribute
{
    public int MaxItems { get; } = maxItems;
}
