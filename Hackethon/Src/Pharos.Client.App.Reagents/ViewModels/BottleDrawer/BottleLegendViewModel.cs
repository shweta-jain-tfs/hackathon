using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.ViewModels.BottleDrawer;

public class BottleLegendViewModel : ViewModelBase
{
    private List<LegendError> _bottleLegendErrors = new();
    private List<LegendError> _taskCardLegendErrors = new();

    public event Action? ResetLegendSelectionRequested;

    public List<BottleStatus> BottleStatuses
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(BottleStatuses));
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

    public BottleStatus? SelectedLegendStatus
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

    public void UpdateLegend(ICollection<BottlePosition> bottlePositions)
    {
        SetLegendStatus(bottlePositions);
        SetLegendErrorStatus(bottlePositions);
        BuildLegendErrors();
    }

    public void UpdateTaskCardLegend(IEnumerable<TaskCard> taskCards)
    {
        _taskCardLegendErrors = taskCards
            .Where(taskCard => taskCard.CardType == TaskCardType.BottleDrawerOpen)
            .Select(_ => new LegendError
            {
                Error = BottleError.CloseDrawer,
                Text = BottleError.CloseDrawer.ToLocalizedString("BottleLegend"),
                IsClickable = false,
                Count = 1,
                GroupOrder = 2,
                Order = (int)BottleError.CloseDrawer
            })
            .ToList();

        BuildLegendErrors();
    }

    public void OnLegendSelectionChanged(LegendSelectionResult? result)
    {
        LegendSelectionResult = result ?? new LegendSelectionResult();
        SelectedLegendStatus = result?.SelectedStatus is BottleStatus status ? status : null;
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

    public static string GetBottleStatusText(BottleStatus status)
    {
        return status switch
        {
            BottleStatus.InUse => ReagentPositionStatus.InUse.ToLocalizedString("Reagent"),
            BottleStatus.Empty => ReagentPositionStatus.Empty.ToLocalizedString("Reagent"),
            _ => status.ToString()
        };
    }

    private void SetLegendStatus(ICollection<BottlePosition> bottlePositions)
    {
        var statuses = new List<BottleStatus>();

        if (bottlePositions.Count != 0)
        {
            var allBottleStatuses = bottlePositions
                .SelectMany(bp => bp.Bottles.Select(b => b.Status))
                .Where(IsLegendStatus)
                .Distinct()
                .Select(status => status.ToBottleStatus())
                .Where(bs => bs.HasValue)
                .Select(bs => bs!.Value);

            statuses.AddRange(allBottleStatuses);
        }

        BottleStatuses = statuses.Distinct().OrderBy(s => (int)s).ToList();
    }

    private void SetLegendErrorStatus(ICollection<BottlePosition> bottlePositions)
    {
        var errors = new List<LegendError>();

        if (bottlePositions.Count != 0)
        {
            var bottleErrors = bottlePositions
                .SelectMany(bp => bp.Bottles.Select(b => b.Status))
                .Where(HasError)
                .GroupBy(status => status)
                .Select(group => new LegendError
                {
                    Error = group.Key,
                    Text = group.Key.ToLocalizedString("BottleLegend"),
                    IsClickable = false,
                    Count = group.Count(),
                    GroupOrder = 1,
                    Order = (int)group.Key
                });

            errors.AddRange(bottleErrors);
        }

        _bottleLegendErrors = errors.ToList();
    }

    private void BuildLegendErrors()
    {
        LegendErrors = [.._bottleLegendErrors, .._taskCardLegendErrors];
    }

    private static bool IsLegendStatus(ReagentPositionStatus status)
    {
        return status is ReagentPositionStatus.InUse or ReagentPositionStatus.Empty;
    }

    private static bool HasError(ReagentPositionStatus status)
    {
        return status is ReagentPositionStatus.UndetectedVolume
               or ReagentPositionStatus.DisconnectedBottle
               or ReagentPositionStatus.UnexpectedVolume
               or ReagentPositionStatus.UnknownBottle
               or ReagentPositionStatus.Expired;
    }
}
