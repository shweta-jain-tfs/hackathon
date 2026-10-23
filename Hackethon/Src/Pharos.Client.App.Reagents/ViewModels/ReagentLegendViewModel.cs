using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Components.Reagent;

namespace Pharos.Client.App.Reagents.ViewModels;

public class ReagentLegendViewModel : ViewModelBase
{
    private List<LegendError> _reagentLegendErrors = new();
    private List<LegendError> _taskCardLegendErrors = new();

    public List<ReagentStatus> ReagentStatuses
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(ReagentStatuses));
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

    public ReagentPositionStatus? SelectedLegendStatus
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedLegendStatus));
            SelectedStatusChanged?.Invoke();
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

    public event Action? SelectedStatusChanged;

    public event Action? RemoveLegendSelection;

    public void OnStatusSelected(LegendSelectionResult? result)
    {
        LegendSelectionResult = result ?? new LegendSelectionResult();

        if (!LegendSelectionResult.AnyItemSelected())
        {
            SelectedLegendStatus = null;
            return;
        }

        SetSelectedStatus(LegendSelectionResult);
    }

    public void UpdateLegend(ICollection<ReagentLoader> reagentLoaders, ICollection<DilutionPlateStack> dilutionPlateStacks, bool needEmptyLoader)
    {
        SetLegendStatus(reagentLoaders, dilutionPlateStacks);
        SetLegendErrorStatus(reagentLoaders, dilutionPlateStacks, needEmptyLoader);
        BuildLegendErrors();
        ClearSelectionIfSelectedItemIsNotAvailable();
    }

    public void UpdateTaskCardLegend(IEnumerable<TaskCard> taskCards)
    {
        _taskCardLegendErrors = taskCards
            .Where(taskCard => taskCard.CardType == TaskCardType.ReagentDrawerOpen)
            .Select(_ => new LegendError
            {
                Error = ReagentError.CloseDrawer,
                Text = ReagentError.CloseDrawer.ToLocalizedString("ReagentLegend"),
                IsClickable = false,
                Count = 1,
                GroupOrder = 3,
                Order = (int)ReagentError.CloseDrawer
            })
            .ToList();

        BuildLegendErrors();
    }

    public static string GetReagentStatusText(ReagentStatus status)
    {
        return status.ToLocalizedString("Reagent");
    }

    private void SetLegendErrorStatus(ICollection<ReagentLoader> reagentLoaders, ICollection<DilutionPlateStack> dilutionPlateStacks, bool needEmptyLoader)
    {
        var errors = new List<LegendError>();

        if (reagentLoaders.Count != 0)
        {
            var reagentCells = GetReagentCellContents(reagentLoaders).ToList();
            var reagentCellStatuses = reagentCells.Select(cell => cell.Status);

            errors.AddRange(CreateLegendErrors(reagentCellStatuses, IsReagentError, isClickable: true, groupOrder: 1));
            var unloadRequestedCount = reagentCells.Count(cell => cell.IsUnloadRequested);
            if (unloadRequestedCount > 0)
            {
                errors.Add(CreateLegendError(ReagentError.UnloadRequested, unloadRequestedCount, true, groupOrder: 4));
            }
        }

        if (dilutionPlateStacks.Count != 0)
        {
            var dilutionErrors = dilutionPlateStacks
                .Where(stack => IsReagentError(stack.Status))
                .GroupBy(stack => stack.Status)
                .Select(group => CreateLegendError(group.Key, group.Count(), true, groupOrder: 2));

            errors.AddRange(dilutionErrors);
        }

        if (needEmptyLoader)
        {
            errors.Add(CreateLegendError(ReagentError.NeedEmptyLoader, 1, false, groupOrder: 5));
        }

        _reagentLegendErrors = errors
            .GroupBy(error => new { error.Error, error.Text, error.IsClickable })
            .Select(group => new LegendError
            {
                Error = group.Key.Error,
                Text = group.Key.Text,
                IsClickable = group.Key.IsClickable,
                Count = group.Sum(error => error.Count),
                GroupOrder = group.Min(error => error.GroupOrder),
                Order = (int)group.Key.Error
            })
            .ToList();
    }

    private void BuildLegendErrors()
    {
        LegendErrors = [.._reagentLegendErrors, .._taskCardLegendErrors];
    }

    private void SetLegendStatus(ICollection<ReagentLoader> reagentLoaders, ICollection<DilutionPlateStack> dilutionPlateStacks)
    {
        var statuses = new List<ReagentStatus>();

        if (reagentLoaders.Count != 0)
        {
            statuses.AddRange(GetReagentCellContents(reagentLoaders)
                .Select(cell => cell.Status)
                .Where(IsValid)
                .Distinct()
                .Select(GetReagentStatus));
        }

        if (dilutionPlateStacks.Count != 0)
        {
            statuses.AddRange(dilutionPlateStacks
                .Select(stack => stack.Status)
                .Where(IsValid)
                .Distinct()
                .Select(GetReagentStatus));
        }

        ReagentStatuses = statuses.Distinct().ToList();

    }

    private static ReagentStatus GetReagentStatus(ReagentPositionStatus status)
    {
        return status switch
        {
            ReagentPositionStatus.Identified => ReagentStatus.Identified,
            ReagentPositionStatus.NotIdentified => ReagentStatus.NotIdentified,
            _ => default
        };

    }

    private static bool IsValid(ReagentPositionStatus status)
    {
        return status is ReagentPositionStatus.Identified or ReagentPositionStatus.NotIdentified;
    }

    private static IEnumerable<CompartmentCellContent> GetReagentCellContents(IEnumerable<ReagentLoader> reagentLoaders)
    {
        return reagentLoaders
            .SelectMany(loader => loader.Compartments)
            .SelectMany(reagent => reagent.CellContent);
    }

    private static IEnumerable<LegendError> CreateLegendErrors(
        IEnumerable<ReagentPositionStatus> statuses,
        Func<ReagentPositionStatus, bool> predicate,
        bool isClickable,
        int groupOrder)
    {
        return statuses
            .Where(predicate)
            .GroupBy(status => status)
            .Select(group => CreateLegendError(group.Key, group.Count(), isClickable, groupOrder));
    }

    private static bool IsReagentError(ReagentPositionStatus status)
    {
        return status is ReagentPositionStatus.UnknownArticle
               or ReagentPositionStatus.UnreadableBarcode
               or ReagentPositionStatus.TransferNotPossible
               or ReagentPositionStatus.DilutionPlateNotDetected
               or ReagentPositionStatus.DilutionPlateNotPickedUp
               or ReagentPositionStatus.InternalStorageFull;
    }

    private static LegendError CreateLegendError(ReagentPositionStatus status, int count, bool isClickable, int groupOrder)
    {
        var reagentError = GetReagentError(status);

        return new LegendError
        {
            Error = reagentError,
            Text = reagentError.ToLocalizedString("ReagentLegend"),
            IsClickable = isClickable,
            Count = count,
            GroupOrder = groupOrder,
            Order = (int)reagentError
        };
    }

    private static LegendError CreateLegendError(ReagentError reagentError, int count, bool isClickable, int groupOrder)
    {
        return new LegendError
        {
            Error = reagentError,
            Text = reagentError.ToLocalizedString("ReagentLegend"),
            IsClickable = isClickable,
            Count = count,
            GroupOrder = groupOrder,
            Order = (int)reagentError
        };
    }

    private static ReagentError GetReagentError(ReagentPositionStatus status)
    {
        return status switch
        {
            ReagentPositionStatus.InternalStorageFull => ReagentError.InternalStorageFull,
            ReagentPositionStatus.UnreadableBarcode => ReagentError.UnreadableBarcode,
            ReagentPositionStatus.TransferNotPossible => ReagentError.TransferNotPossible,
            ReagentPositionStatus.UnknownArticle => ReagentError.UnknownArticle,
            ReagentPositionStatus.DilutionPlateNotDetected => ReagentError.DilutionPlateNotDetected,
            ReagentPositionStatus.DilutionPlateNotPickedUp => ReagentError.DilutionPlateNotPickedUp,
            ReagentPositionStatus.UnloadRequested => ReagentError.UnloadRequested,
            _ => default
        };
    }

    private static ReagentPositionStatus? GetReagentPositionStatus(ReagentStatus reagentStatus)
    {
        return reagentStatus switch
        {
            ReagentStatus.Identified => ReagentPositionStatus.Identified,
            ReagentStatus.NotIdentified => ReagentPositionStatus.NotIdentified,
            _ => default(ReagentPositionStatus?)
        };
    }

    private static ReagentPositionStatus? GetReagentPositionStatus(ReagentError reagentError)
    {
        return reagentError switch
        {
            ReagentError.InternalStorageFull => ReagentPositionStatus.InternalStorageFull,
            ReagentError.UnreadableBarcode => ReagentPositionStatus.UnreadableBarcode,
            ReagentError.TransferNotPossible => ReagentPositionStatus.TransferNotPossible,
            ReagentError.UnknownArticle => ReagentPositionStatus.UnknownArticle,
            ReagentError.DilutionPlateNotDetected => ReagentPositionStatus.DilutionPlateNotDetected,
            ReagentError.DilutionPlateNotPickedUp => ReagentPositionStatus.DilutionPlateNotPickedUp,
            ReagentError.UnloadRequested => ReagentPositionStatus.UnloadRequested,
            _ => default(ReagentPositionStatus?)
        };
    }

    private void SetSelectedStatus(LegendSelectionResult result)
    {
        if (result.SelectedStatus is ReagentStatus reagentStatus)
        {
            SelectedLegendStatus = GetReagentPositionStatus(reagentStatus);
        }
        else if (result.SelectedError?.Error is ReagentError errorReagentStatus)
        {
            SelectedLegendStatus = GetReagentPositionStatus(errorReagentStatus);
        }
        else
        {
            SelectedLegendStatus = null;
        }
    }

    private void ClearSelectionIfSelectedItemIsNotAvailable()
    {
        if (!LegendSelectionResult.AnyItemSelected())
        {
            return;
        }

        if (IsSelectedLegendItemAvailable())
        {
            return;
        }

        LegendSelectionResult = new LegendSelectionResult();
        SelectedLegendStatus = null;
        RemoveLegendSelection?.Invoke();
    }

    private bool IsSelectedLegendItemAvailable()
    {
        if (LegendSelectionResult.SelectedStatus is ReagentStatus reagentStatus)
        {
            return ReagentStatuses.Contains(reagentStatus);
        }

        if (LegendSelectionResult.SelectedError?.Error is ReagentError reagentError)
        {
            return LegendErrors.Any(error => Equals(error.Error, reagentError));
        }

        return false;
    }

    public void ClearLegendSelection()
    {
        if (!LegendSelectionResult.AnyItemSelected() && SelectedLegendStatus is null)
        {
            return;
        }

        LegendSelectionResult = new LegendSelectionResult();
        SelectedLegendStatus = null;
        RemoveLegendSelection?.Invoke();
    }
}
