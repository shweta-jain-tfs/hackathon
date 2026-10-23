namespace Pharos.Client.Common.App.ViewModels;

public enum LockScreenStateInfoItemType
{
    Sample,
    InstrumentState,
    Test
}

public sealed record LockScreenStateInfoItem(
    LockScreenStateInfoItemType Type,
    string? HeaderText,
    string? PrimaryText,
    string? SecondaryText,
    int? Hours,
    string? HoursUnit,
    int? Minutes,
    string? MinutesUnit,
    string? TimeText);
