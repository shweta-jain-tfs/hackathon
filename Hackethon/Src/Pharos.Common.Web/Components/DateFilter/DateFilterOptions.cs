namespace Pharos.Common.Web.Components.DateFilter;

[Flags]
public enum DateFilterOptions
{
    None = 0,
    Today = 1 << 0,
    LastDays = 1 << 1,
    DateRange = 1 << 2,
    Month = 1 << 3,

    Default = Today | LastDays | DateRange,
    All = Default | Month
}