using Pharos.Common.Web.Components.Icons;
using Pharos.Common.Web.Components.Legend;
using Pharos.Common.Web.Utilities;

namespace Pharos.Common.Web.Components.Drawer;

public partial class PhSampleTube
{
    [Parameter]
    [EditorRequired]
    public TubeStatus? Status { get; set; }

    [Parameter]
    public bool IsHighPriority { get; set; }

    [Parameter]
    public bool IsManualInput { get; set; }

    [Parameter]
    public bool IsConfirmManualInput { get; set; }

    [Parameter]
    public bool IsUnloadRequested { get; set; }

    [Parameter]
    public SampleTubeSize Size { get; set; }

    [Parameter]
    public TubeError Error { get; set; }

    [CascadingParameter(Name = "SampleDrawerLegendSelectionResult")]
    public LegendSelectionResult? SampleDrawerLegendSelectionResult { get; set; }

    [CascadingParameter(Name = "TrayPanelLegendSelectionResult")]
    public LegendSelectionResult? TrayPanelLegendSelectionResult { get; set; }


    private string TubeContainerClass { get; set; } = string.Empty;
    private string PrimaryIconPlaceholderClass { get; set; } = string.Empty;
    private string ManualEntryClass { get; set; } = string.Empty;
    private string UnloadRequestedClass { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        TubeContainerClass = new CssBuilder("ph-sample-tube")
           .AddClass("ph-sample-tube--small", Size == SampleTubeSize.Small)
           .AddClass("ph-sample-tube--medium", Size == SampleTubeSize.Medium)
           .AddClass("ph-sample-tube--large", Size == SampleTubeSize.Large)
           .AddClass("ph-sample-tube--error", Error != TubeError.None)
           .AddClass("ph-sample-tube--high-priority", IsHighPriority)
           .AddClass("ph-sample-tube--dim", IsAnyLegendItemSelected() && !IsTubeMatchesWithSelectedLegend())
           .AddClass("ph-sample-tube--highlighted", IsTubeMatchesWithSelectedLegend())
           .Build();

        PrimaryIconPlaceholderClass = new CssBuilder("ph-sample-tube__primary-icon-placeholder")
            .AddClass("ph-sample-tube__primary-icon-placeholder--none", Status is null)
            .AddClass("ph-sample-tube__primary-icon-placeholder--empty", Status == TubeStatus.Empty)
            .AddClass("ph-sample-tube__primary-icon-placeholder--not-identified", Status == TubeStatus.NotIdentified)
            .AddClass("ph-sample-tube__primary-icon-placeholder--in-progress", Status == TubeStatus.InProgress)
            .AddClass("ph-sample-tube__primary-icon-placeholder--completed", Status == TubeStatus.Completed)
            .AddClass("ph-sample-tube__primary-icon-placeholder--partly-completed", Status == TubeStatus.PartlyCompleted)
            .AddClass("ph-sample-tube__primary-icon-placeholder--follow-up-reflex", Status == TubeStatus.FollowUpReflex)
            .AddClass("ph-sample-tube__primary-icon-placeholder--pipetted-qc", Status == TubeStatus.PipettedQc)
            .Build();

        ManualEntryClass = new CssBuilder("ph-sample-tube__primary-icon-placeholder--manual-entry")
            .AddClass("ph-sample-tube__primary-icon-placeholder--blink", IsConfirmManualInput)
            .Build();

        UnloadRequestedClass = new CssBuilder("ph-sample-tube__primary-icon-placeholder--unload-requested")
            .Build();

        base.OnParametersSet();
    }

    public StatusIconSize StatusIconSize => Size switch
    {
        SampleTubeSize.Small => StatusIconSize.Md,
        SampleTubeSize.Medium => StatusIconSize.Lg,
        _ => StatusIconSize.Md
    };

    private bool IsTubeMatchesWithSelectedLegend()
    {
        var legendSelectionResult = GetActiveLegendSelectionResult();

        if (!(legendSelectionResult?.AnyItemSelected() == true))
        {
            return false;
        }

        if ((IsManualInput || IsConfirmManualInput) && legendSelectionResult!.IsManualEntrySelected)
        {
            return true;
        }

        if (IsHighPriority && legendSelectionResult!.IsHighPrioritySelected)
        {
            return true;
        }

        if (IsUnloadRequested && legendSelectionResult!.IsUnloadRequestedSelected)
        {
            return true;
        }

        // Status match (only if no error)
        if (legendSelectionResult.SelectedStatus is TubeStatus selectedStatus)
        {
            if (Error == TubeError.None && Status == selectedStatus)
            {
                return true;
            }
        }

        // Error match
        if (legendSelectionResult.SelectedError?.Error is TubeError selectedError)
        {
            if (Error == selectedError)
            {
                return true;
            }
        }

        if (legendSelectionResult.SelectedError?.Error is TubeStatus selectedStatusIssue)
        {
            if (Error == TubeError.None && Status == selectedStatusIssue)
            {
                return true;
            }
        }


        return false;
    }

    private bool IsAnyLegendItemSelected()
    {
        var legendSelectionResult = GetActiveLegendSelectionResult();
        return legendSelectionResult?.AnyItemSelected() ?? false;
    }

    private LegendSelectionResult? GetActiveLegendSelectionResult()
    {
        if (SampleDrawerLegendSelectionResult?.AnyItemSelected() == true)
        {
            return SampleDrawerLegendSelectionResult;
        }
        else if (TrayPanelLegendSelectionResult?.AnyItemSelected() == true)
        {
            return TrayPanelLegendSelectionResult;
        }
        else
        {
            return null;
        }

    }

}

public enum TubeStatus
{
    Empty = 0,
    NotIdentified = 1,
    InProgress = 2,
    Completed = 3,
    PartlyCompleted = 4,
    PipettedQc = 5,
    FollowUpReflex = 6
}

public enum TubePositionSource
{

    Unspecified = 0,

    Instrument = 1,

    ManualInput = 2,

    LisEntry = 3,

    ConfirmManualInput = 4,

}

/// <summary>
/// Specifies the error of sample tube.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries.
/// </remarks>
public enum TubeError
{
    None,
    TubeLoadedInWrongDrawer,
    TubeShapeError,
    TubeGripError,
    UnreadableBarcode,
    InvalidBarcode,
    DuplicateBarcode,
    MissingTestOrders,
    MethodsNotActive,
    QcNotNeeded,
    NoSeparationDetected,
    ClotDetected,
    SampleShortage,
    VolumeMeasurementError,
    PipettingError,
    BarcodeMismatchError,
    QcOutOfRange,
    MultipleBarcodes
}

public enum SampleType
{
    None = 0,
    Sample = 1,
    QualityControl = 2,
    Calibrator = 3,
    CurveControl = 4
}
