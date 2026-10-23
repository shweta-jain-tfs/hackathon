namespace Pharos.Server.Application.Settings.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ValueAfterCopy : Attribute
{
    public object Value { get; }

    public ValueAfterCopy(object value)
    {
        Value = value;
    }
}
