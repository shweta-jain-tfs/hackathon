namespace Pharos.Server.Domain.Enums;

/// <summary>
/// This enumeration lists all possible checks for the manufacturing limits
/// </summary>
public enum ManufacturingLimitCheck
{
    None,
    LessThan_GreaterThan,
    LessThanOrEqual_GreaterThanOrEqual,
    LessThanOrEqual_GreaterThan,
    LessThan_GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual
}
