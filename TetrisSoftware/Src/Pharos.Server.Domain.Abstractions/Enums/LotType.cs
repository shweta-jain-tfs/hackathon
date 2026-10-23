namespace Pharos.Server.Domain.Enums;

/// <summary>
/// This enumeration lists all method quality lot types available
/// </summary>
public enum LotType
{
    UseDefaultQcValues = 0,
    AcceptanceRange = 1,
    TargetValue = 2,
    Pending = 3
}