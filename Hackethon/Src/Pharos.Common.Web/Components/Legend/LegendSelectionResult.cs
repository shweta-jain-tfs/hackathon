namespace Pharos.Common.Web.Components.Legend;

public class LegendSelectionResult
{
    public object? SelectedStatus { get; set; }
    public LegendError? SelectedError { get; set; }
    public bool IsHighPrioritySelected { get; set; }
    public bool IsManualEntrySelected { get; set; }
    public bool IsUnloadRequestedSelected { get; set; }

    public bool AnyItemSelected()
    {
        return SelectedStatus is not null ||
               SelectedError?.Error is not null ||
               IsManualEntrySelected ||
               IsHighPrioritySelected ||
               IsUnloadRequestedSelected;
    }
}