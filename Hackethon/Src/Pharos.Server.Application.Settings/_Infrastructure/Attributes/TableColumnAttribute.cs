namespace Pharos.Server.Application.Settings.Attributes;

/// <summary>
/// Mark a property with this attribute within a PageList to have it as column
/// in the PageList table. Only works for properties that are in the PageList class, NOT for subclasses of that.
/// The type of the marked property must be of an Option type, i.e. int, double, string, enum, date.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TableColumnAttribute : Attribute
{
}
