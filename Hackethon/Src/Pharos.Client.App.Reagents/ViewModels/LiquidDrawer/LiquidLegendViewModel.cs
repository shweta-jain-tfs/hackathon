using Pharos.Common.Web.Components.Legend;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.ViewModels.LiquidDrawer;

public class LiquidLegendViewModel : ViewModelBase
{
    public event Action? ResetLegendSelectionRequested;

    public List<LiquidBottleStatus> LegendStatuses
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(LegendStatuses));
        }
    } = new();

    public List<LegendError> LegendErrors
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(LegendErrors));
        }
    } = new();

    public LiquidBottleStatus? SelectedLegendStatus
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedLegendStatus));
        }
    }

    public LegendSelectionResult LegendSelectionResult
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(LegendSelectionResult));
        }
    } = new();

    public void UpdateLegend(ICollection<LiquidContainer> containers)
    {
        var mappedStatuses = containers
            .Select(container => container.ToLiquidBottleStatus())
            .Where(status => status.HasValue)
            .Select(status => status!.Value)
            .Distinct()
            .ToList();

        LegendStatuses = mappedStatuses;
        ClearSelectionIfSelectedItemIsNotAvailable();
    }

    public void UpdateTaskCardLegend(IEnumerable<TaskCard> taskCards)
    {
        LegendErrors = taskCards
            .Where(taskCard => taskCard.CardType == TaskCardType.LiquidDrawerOpen)
            .Select(_ => new LegendError
            {
                Error = LiquidError.CloseDrawer,
                Text = LiquidError.CloseDrawer.ToLocalizedString("LiquidLegend"),
                IsClickable = false,
                Count = 1,
                GroupOrder = 1,
                Order = (int)LiquidError.CloseDrawer
            })
            .ToList();
    }

    public void OnLegendSelectionChanged(LegendSelectionResult? result)
    {
        LegendSelectionResult = result ?? new LegendSelectionResult();
        SelectedLegendStatus = result?.SelectedStatus is LiquidBottleStatus status ? status : null;
    }

    public void ClearLegendSelection()
    {
        if (!LegendSelectionResult.AnyItemSelected())
        {
            return;
        }

        LegendSelectionResult = new LegendSelectionResult();
        SelectedLegendStatus = null;
        ResetLegendSelectionRequested?.Invoke();
    }

    private void ClearSelectionIfSelectedItemIsNotAvailable()
    {
        if (LegendSelectionResult.SelectedStatus is not LiquidBottleStatus selectedStatus)
        {
            return;
        }

        if (LegendStatuses.Contains(selectedStatus))
        {
            return;
        }

        ClearLegendSelection();
    }

}
