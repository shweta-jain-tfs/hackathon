namespace Pharos.Common.Web.Contexts;

public record DataGridStrings
{
    public string ClearAllFilters { get; set; } = string.Empty;
    public string ClearFilter { get; init; } = string.Empty;
    public string CustomRange { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string LastDaysPrefix { get; set; } = string.Empty;
    public string LastDaysSuffix { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public string ProgressBarText { get; init; } = string.Empty;
    public string ResetToDefault { get; set; } = string.Empty;
    public string SelectedText { get; set; } = string.Empty;
    public string Show { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Today { get; set; } = string.Empty;


    public static DataGridStrings Default => new()
    {
        ClearAllFilters = Missing(nameof(ClearAllFilters)),
        ClearFilter = Missing(nameof(ClearFilter)),
        CustomRange = Missing(nameof(CustomRange)),
        From = Missing(nameof(From)),
        LastDaysPrefix = Missing(nameof(LastDaysPrefix)),
        LastDaysSuffix = Missing(nameof(LastDaysSuffix)),
        Month = Missing(nameof(Month)),
        ProgressBarText = Missing(nameof(ProgressBarText)),
        ResetToDefault = Missing(nameof(ResetToDefault)),
        SelectedText = Missing(nameof(SelectedText)),
        Show = Missing(nameof(Show)),
        To = Missing(nameof(To)),
        Today = Missing(nameof(Today)),
    };

    private static string Missing(string name)
    {
        return $"[{nameof(DataGridStrings)}.{name}]";
    }
}
