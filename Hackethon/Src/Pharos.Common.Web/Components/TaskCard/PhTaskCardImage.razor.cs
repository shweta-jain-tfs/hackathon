using Pharos.Common.Web.Components.TaskCard.Svg;

namespace Pharos.Common.Web.Components.TaskCard;

public partial class PhTaskCardImage
{
    [Parameter] public TaskCardImageType ImageType { get; set; }

    private static readonly Dictionary<TaskCardImageType, Type> _imageComponents = new()
    {
        [TaskCardImageType.BottleDrawerError] = typeof(TaskCard_BottleError),
        [TaskCardImageType.BottleDrawerOpen] = typeof(TaskCard_BottleError),
        [TaskCardImageType.CalibrationIssue] = typeof(TaskCard_CalibrationIssue),
        [TaskCardImageType.CheckLiquidDrawer] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.CheckWasteDrawer] = typeof(TaskCard_SolidWasteError),
        [TaskCardImageType.Error] = typeof(TaskCard_Error),
        [TaskCardImageType.HumidityOutOfRange] = typeof(TaskCard_HumidityOutOfRange),
        [TaskCardImageType.InventoryCarrierError] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.InventoryCartridgeError] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.InstrumentSoftwareOffline] = typeof(TaskCard_Error),
        [TaskCardImageType.LiquidBottleError] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.LiquidDrawerOpen] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.LiquidEmptyWaste] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.LiquidRefillRinse] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.ConnectionFailure] = typeof(TaskCard_ConnectionFailure),
        [TaskCardImageType.LiquidRinseBottleMissing] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.LiquidWasteBottleMissing] = typeof(TaskCard_LiquidError),
        [TaskCardImageType.LoadEmptyLeftSampleTray] = typeof(TaskCard_LoadEmptyLeftSampleTray),
        [TaskCardImageType.LoadEmptyRightSampleTray] = typeof(TaskCard_LoadEmptyRightSampleTray),
        [TaskCardImageType.LoadEmptySampleTray] = typeof(TaskCard_SampleError),
        [TaskCardImageType.MaintenanceCleaning] = typeof(TaskCard_MaintenanceCleaning),
        [TaskCardImageType.MaintenanceManualCleaning] = typeof(TaskCard_MaintenanceManualCleaning),
        [TaskCardImageType.NeedEmptyLoader] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.QCIssue] = typeof(TaskCard_QCIssue),
        [TaskCardImageType.ReagentDrawerError] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.ReagentDrawerOpen] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.RemoveReagentsFromDrawer] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.ReagentLoaderWithError] = typeof(TaskCard_ReagentError),
        [TaskCardImageType.RemoveSamplesFromDrawer] = typeof(TaskCard_SampleError),
        [TaskCardImageType.ResultsIssue] = typeof(TaskCard_ResultIssue),
        [TaskCardImageType.ResultReviewPending] = typeof(TaskCard_ResultReviewPending),
        [TaskCardImageType.SampleDrawerOpen] = typeof(TaskCard_SampleError),
        [TaskCardImageType.SamplePartlyCompleted] = typeof(TaskCard_SampleError),
        [TaskCardImageType.SampleTrayError] = typeof(TaskCard_SampleError),
        [TaskCardImageType.SampleTubeError] = typeof(TaskCard_SampleError),
        [TaskCardImageType.SolidWasteBiohazardBinMissing] = typeof(TaskCard_SolidWasteError),
        [TaskCardImageType.SolidWasteDrawerOpen] = typeof(TaskCard_SolidWasteError),
        [TaskCardImageType.SolidWasteEmpty] = typeof(TaskCard_SolidWasteError),
        [TaskCardImageType.SolidWasteRecyclableBinMissing] = typeof(TaskCard_SolidWasteError),
        [TaskCardImageType.TemperatureOutOfRange] = typeof(TaskCard_TemperatureOutOfRange),
        [TaskCardImageType.UpdateFailed] = typeof(TaskCard_UpdateFailed),
        [TaskCardImageType.UpdatesAvailable] = typeof(TaskCard_UpdatesAvailable),
        [TaskCardImageType.UpdateSuccessful] = typeof(TaskCard_UpdateSuccessful),
        [TaskCardImageType.UrgentLoading] = typeof(TaskCard_UrgentLoading),
        [TaskCardImageType.Generic] = typeof(TaskCard_Generic),
        [TaskCardImageType.RequestReportResult] = typeof(TaskCard_RequestReportResult),
        [TaskCardImageType.RequestSampleIssue] = typeof(TaskCard_RequestSampleIssue)
    };

    private Type ImageComponentType => _imageComponents.GetValueOrDefault(ImageType, typeof(TaskCard_Generic));
}
