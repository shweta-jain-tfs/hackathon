using Pharos.Client.Common.Models;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public static class ProcessListLogic
{
    private static readonly InstrumentState[] _assayRelatedStatuses =
    [
        InstrumentState.Assay,
        InstrumentState.AssayCmr,
        InstrumentState.AssayFinishing,
        InstrumentState.AssayFinishOrdered
    ];

    /// <summary>
    /// Determines if the process time should be displayed for a process list item.
    /// Returns true if the instrument is in an assay-related state and the item's time has a value.
    /// </summary>
    public static bool ShouldDisplayProcessTime(ProcessListItem item, InstrumentState? state)
    {
        if (!item.Time.HasValue ||
            item.Time.Value <= DateTimeOffset.MinValue ||
            item.Time.Value.Year <= 1970)
        {
            return false;
        }

        return state.HasValue && _assayRelatedStatuses.Contains(state.Value);
    }
}