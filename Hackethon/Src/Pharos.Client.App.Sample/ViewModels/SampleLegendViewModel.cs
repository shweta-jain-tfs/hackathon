using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Components.Legend;

namespace Pharos.Client.App.Sample.ViewModels;

public class SampleLegendViewModel(ITaskCardTranslator taskCardTranslator, IStringLocalizer<Translations> localization) : ViewModelBase
{
    private List<LegendError> _tubeErrorLegendErrors = [];
    private List<LegendError> _taskCardLegendErrors = [];
    private static readonly TaskCardType[] _taskCardTypes =
    [
        TaskCardType.LoadEmptySampleTray,
        TaskCardType.LoadEmptyLeftSampleTray,
        TaskCardType.LoadEmptyRightSampleTray,
        TaskCardType.SampleDrawerOpen
    ];

    private static readonly SampleTubeStatus[] _issueStatuses =
    {
       SampleTubeStatus.PartlyCompleted
    };

    public event Action? ResetLegendSelectionRequested;

    public List<TubeStatus> LegendStatuses
    {
        get;
        private set
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

    public bool IsAnyHighPriority
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsAnyHighPriority));
        }
    }

    public bool IsAnyManualEntry
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsAnyManualEntry));
        }
    }

    public int UnloadRequestedCount
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(UnloadRequestedCount));
        }
    }

    public LegendSelectionResult LegendSelectionResult
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(LegendSelectionResult));
        }
    } = new();

    public void UpdateLegend(List<SampleTrayDetails> sampleTrayDetails)
    {
        if (sampleTrayDetails is null)
        {
            return;
        }
        SetStatusesItems(sampleTrayDetails);
        SetErrorItems(sampleTrayDetails);
        BuildLegendErrors();
    }

    public void UpdateTaskCardLegend(List<Pharos.Client.Common.Models.TaskCard> taskCards)
    {
        _taskCardLegendErrors = taskCards
            .Where(tc => _taskCardTypes.Contains(tc.CardType) && tc.CardState != TaskCardState.Hidden)
            .Select(tc => new LegendError
            {
                Error = tc.CardType,
                Text = GetTaskCardLegendText(tc.CardType),
                IsClickable = false,
                Count = 1,
                GroupOrder = 2,
                Order = (int)tc.CardType
            })
            .ToList();

        BuildLegendErrors();
    }

    public void OnLegendSelectionChanged(LegendSelectionResult result)
    {
        LegendSelectionResult = result;
    }

    public void OnResetLegendSelectionRequested()
    {
        ResetLegendSelectionRequested?.Invoke();
    }

    public bool HasLegendItems()
    {
        return LegendStatuses.Count > 0 || IsAnyHighPriority || IsAnyManualEntry || UnloadRequestedCount > 0 || LegendErrors.Count > 0;
    }

    private void SetStatusesItems(List<SampleTrayDetails> sampleTrayDetails)
    {
        var positions = GetTubePositions(sampleTrayDetails);

        IsAnyHighPriority = positions.Any(position => position.IsHighPrioritySet);

        IsAnyManualEntry = positions.Any(position =>
            position.SamplePositionSource == SampleTubeIdentitySource.ManualInput ||
            position.SamplePositionSource == SampleTubeIdentitySource.ConfirmManualInput);

        LegendStatuses = positions
            .Where(position => IsValid(position) && !_issueStatuses.Contains(position.SampleTubeStatus))
            .Select(position => position.SampleTubeStatus.ToTubeStatus())
            .Distinct()
            .ToList();
    }

    private void SetErrorItems(List<SampleTrayDetails> sampleTrayDetails)
    {
        var positions = GetTubePositions(sampleTrayDetails);

        UnloadRequestedCount = positions.Count(x => x.IsUnloadRequested);

        var legendErrors = new List<LegendError>();

        var positionErrorGroups = positions
                      .Where(HasError)
                      .GroupBy(position => position.Error)
                      .Select(group => new { Error = group.Key, Count = group.Count() })
                      .ToList();

        foreach (var positionErrorGroup in positionErrorGroups)
        {
            var error = positionErrorGroup.Error.ToTubeError();

            legendErrors.Add(new LegendError
            {
                Error = error,
                Text = positionErrorGroup.Error.ToLocalizedString("Samples"),
                IsClickable = true,
                Count = positionErrorGroup.Count,
                GroupOrder = 1,
                Order = (int)error
            });
        }

        var issuePositions = positions
            .Where(IsValid)
            .Where(position => _issueStatuses.Contains(position.SampleTubeStatus))
            .GroupBy(position => position.SampleTubeStatus)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var issuePositionGroup in issuePositions)
        {
            var status = issuePositionGroup.Key.ToTubeStatus();

            legendErrors.Add(new LegendError
            {
                Error = status,
                Text = GetSampleTubeStatusText(status),
                IsClickable = true,
                Count = issuePositionGroup.Value,
                GroupOrder = 3,
                Order = (int)status
            });
        }

        _tubeErrorLegendErrors = legendErrors;
    }

    private void BuildLegendErrors()
    {
        LegendErrors = [.._tubeErrorLegendErrors, .._taskCardLegendErrors];
    }

    private string GetTaskCardLegendText(TaskCardType taskCardType)
    {
        return taskCardType == TaskCardType.SampleDrawerOpen
            ? localization.GetString("Legend_TaskCard_SampleDrawerOpen")
            : taskCardTranslator.GetTitle(taskCardType);
    }

    private static List<SampleTrayPosition> GetTubePositions(List<SampleTrayDetails> sampleTrayDetails)
    {
        return sampleTrayDetails
            .Where(tray => !HasError(tray) && tray.SamplePositions != null)
            .SelectMany(tray => tray.SamplePositions)
            .ToList();
    }

    public static string GetSampleTubeStatusText(TubeStatus status)
    {
        return status.ToLocalizedString("Samples");
    }

    private static bool IsValid(SampleTrayPosition position)
    {
        return position != null &&
               position.SampleTubeStatus != SampleTubeStatus.None &&
               position.Error == SampleTubeError.None;
    }

    private static bool HasError(SampleTrayPosition position)
    {
        return position != null && position.Error != SampleTubeError.None;
    }

    private static bool HasError(SampleTrayDetails tray)
    {
        return tray != null && tray.TrayError != SampleTrayError.None;
    }
}
