namespace Pharos.Common.Web.Components.DateFilter;

public sealed record DateFilterValue(
    DateFilterMode Mode = DateFilterMode.None,
    int? LastDays = DateFilterValue.DefaultLastDays,
    DateOnly? From = null,
    DateOnly? To = null)
{
    public const int DefaultLastDays = 3;
    public static DateFilterValue Empty => new(LastDays: null);
}