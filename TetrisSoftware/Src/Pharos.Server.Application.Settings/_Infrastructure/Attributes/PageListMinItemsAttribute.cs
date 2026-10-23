namespace Pharos.Server.Application.Settings.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PageListMinItemsAttribute(int minItems) : Attribute
{
    public int MinItems { get; } = minItems;
}

