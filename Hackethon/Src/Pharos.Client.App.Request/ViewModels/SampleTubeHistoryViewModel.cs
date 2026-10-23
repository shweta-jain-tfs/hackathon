using Pharos.Common.Web.Components.Status;

namespace Pharos.Client.App.Request.ViewModels;

public class SampleTubeHistoryViewModel : ViewModelItem<SampleTubeHistory>
{
    public SampleTubeHistoryViewModel(SampleTubeHistory model) : base(model)
    {
    }

    public DateTimeOffset? LoadTimestamp => Model.LoadTimestamp;
    public string? TubeId => Model.DisplayTubeId;
    public string? InstrumentNumber => Model.InstrumentNumber;
    public string InstrumentText
    {
        get
        {
            if (Model.Area is null or SampleArea.None || string.IsNullOrEmpty(Model.InstrumentName))
            {
                return string.Empty;
            }

            if (Model.Area is SampleArea.InternalStorage or SampleArea.Shuttle1 or SampleArea.Shuttle2 && Model.TubePosition.HasValue)
            {
                return $"{Model.InstrumentName}, {Model.Area.ToLocalizedString()} ({Model.TubePosition!.Value})";
            }

            return $"{Model.InstrumentName}, {Model.Area.ToLocalizedString()}";
        }
    }

    public string TrayPosition => string.IsNullOrEmpty(TrayId) ? string.Empty : $"{TrayId} ({Model.TubePositionDisplayValue})";
    public SampleTubeStatus TubeStatus => Model.TubeStatus;
    public SampleTubeError Error => Model.Error;

    public string GetTubeStatusText()
    {
        var tubeStatus = TubeStatus is SampleTubeStatus.None ? "-" : TubeStatus.ToLocalizedString();

        if (Model.HighPriority)
        {
            tubeStatus += $", {Translations.RequestDetailPage_Tubes_HighPriority}";
        }
        if (Model.ManuallyEntered)
        {
            tubeStatus += $", {Translations.RequestDetailPage_Tubes_ManuallyEntered}";
        }

        return tubeStatus;
    }

    public string GetErrorText()
    {
        var errorTexts = new List<string>();

        if (Error != SampleTubeError.None)
        {
            errorTexts.Add(Error.ToLocalizedString());
        }

        if (Model.UnloadRequested)
        {
            errorTexts.Add(Translations.RequestDetailPage_Tubes_UnloadRequested);
        }

        return string.Join(", ", errorTexts);
    }

    public string? TrayId => Model.TrayId;
    public int? TubePosition => Model.TubePosition;
    public string? ImageIdentity => Model.ImageIdentity;
    public bool ShowImageLink => !string.IsNullOrEmpty(ImageIdentity);
    public bool ShowPositionLink => Model.DoesTrayExist && TubePosition.HasValue;
    public bool ShowInstrumentLink => Model is { IsInCurrentInstrument: true, Area: SampleArea.SampleDrawer1 or SampleArea.SampleDrawer2 };
    public StatusType TubePhStatus => GetStatusType(Model.TubeStatus);
    public StatusType ErrorPhStatus => GetErrorPhStatus();
    public string SampleVolumeToDisplay => Model.SampleVolume.HasValue ? $"{Model.SampleVolume.Value} µL" : string.Empty;

    private static StatusType GetStatusType(SampleTubeStatus tubeStatus)
    {
        return tubeStatus switch
        {
            SampleTubeStatus.None => StatusType.None,
            SampleTubeStatus.NotIdentified => StatusType.WorkflowInProgress,
            SampleTubeStatus.InProgress => StatusType.WorkflowInProgress,
            SampleTubeStatus.Completed => StatusType.WorkflowCompleted,
            SampleTubeStatus.FollowUpReflex => StatusType.WorkflowInProgress,
            SampleTubeStatus.PartlyCompleted => StatusType.WorkflowInProgress,
            SampleTubeStatus.PipettedQc => StatusType.WorkflowInProgress,
            _ => StatusType.None
        };
    }

    private static StatusType GetStatusType(SampleTubeError tubeError)
    {
        return tubeError switch
        {
            SampleTubeError.None => StatusType.None,
            SampleTubeError.TubeLoadedInWrongDrawer => StatusType.Warning,
            SampleTubeError.TubeShapeError => StatusType.Warning,
            SampleTubeError.TubeGripError => StatusType.Warning,
            SampleTubeError.UnreadableBarcode => StatusType.Warning,
            SampleTubeError.ClotDetected => StatusType.Warning,
            SampleTubeError.VolumeMeasurementError => StatusType.Warning,
            SampleTubeError.BarcodeMismatchError => StatusType.Warning,
            SampleTubeError.NoSeparationDetected => StatusType.Warning,
            SampleTubeError.SampleShortage => StatusType.Warning,
            SampleTubeError.DuplicateBarcode => StatusType.Warning,
            SampleTubeError.MissingTestOrders => StatusType.Warning,
            SampleTubeError.QcOutOfRange => StatusType.Warning,
            SampleTubeError.QcNotNeeded => StatusType.Warning,
            SampleTubeError.InvalidBarcode => StatusType.Warning,
            SampleTubeError.MethodsNotActive => StatusType.Warning,
            _ => StatusType.None
        };
    }

    private StatusType GetErrorPhStatus()
    {
        if (Error is not SampleTubeError.None)
        {
            return GetStatusType(Error);
        }

        if (Model.UnloadRequested)
        {
            return StatusType.Info;
        }

        return StatusType.None;
    }
}
