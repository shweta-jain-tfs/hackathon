using Pharos.Server.Application.TaskCard;

namespace Pharos.Server.TaskCard.Application.Services;

internal interface ITaskCardRules
{
    IReadOnlyList<TaskCardType> TaskCardsToRemoveWhenInstrumentIsDeleted();
    bool ValidateInstrumentNumberAndCardType(string? instrumentNumber, TaskCardType cardType);
    bool ShowOnLockScreen(TaskCardType cardType);
    bool ControlsAllInstruments(TaskCardType cardType);
    public IReadOnlyList<TaskCardType> ControlsAllInstruments();
    bool StoreInDatabase(TaskCardType cardType);
}

internal class TaskCardRules : ITaskCardRules
{
    private static readonly List<TaskCardType> _taskCardsToRemoveWhenInstrumentIsDeleted =
    [
        TaskCardType.SampleDrawerOpen,
        TaskCardType.Error,
        TaskCardType.ReagentDrawerOpen,
        TaskCardType.ReagentDrawerError,
        TaskCardType.RemoveReagentsFromDrawer,
        TaskCardType.RemoveSamplesFromDrawer,
        TaskCardType.SampleTrayError,
        TaskCardType.SamplePartlyCompleted,
        TaskCardType.ReagentLoaderWithError,
        TaskCardType.MaintenanceCleaning,
        TaskCardType.MaintenanceManualCleaning,
        TaskCardType.NeedEmptyLoader,
        TaskCardType.LoadEmptySampleTray,
        TaskCardType.InventoryCarrierError,
        TaskCardType.InventoryCartridgeError,
        TaskCardType.LoadEmptyLeftSampleTray,
        TaskCardType.LoadEmptyRightSampleTray,
        TaskCardType.BottleDrawerOpen,
        TaskCardType.BottleDrawerError,
        TaskCardType.UrgentLoading,
        TaskCardType.LiquidDrawerOpen,
        TaskCardType.CheckLiquidDrawer,
        TaskCardType.LiquidEmptyWaste,
        TaskCardType.LiquidRefillRinse,
        TaskCardType.SolidWasteDrawerOpen,
        TaskCardType.SolidWasteEmpty,
        TaskCardType.CheckWasteDrawer,
        TaskCardType.TemperatureOutOfRange,
        TaskCardType.HumidityOutOfRange,
        TaskCardType.SampleTubeError,
        TaskCardType.InstrumentSoftwareOffline,
        TaskCardType.ResultReviewPending
    ];

    private static readonly List<TaskCardType> _taskCardsToShowOnLockScreen =
    [
        TaskCardType.SampleDrawerOpen,
        TaskCardType.Error,
        TaskCardType.ReagentDrawerOpen,
        TaskCardType.ReagentDrawerError,
        TaskCardType.RemoveReagentsFromDrawer,
        TaskCardType.RemoveSamplesFromDrawer,
        TaskCardType.SampleTrayError,
        TaskCardType.SamplePartlyCompleted,
        TaskCardType.ReagentLoaderWithError,
        TaskCardType.MaintenanceCleaning,
        TaskCardType.MaintenanceManualCleaning,
        TaskCardType.NeedEmptyLoader,
        TaskCardType.LoadEmptySampleTray,
        TaskCardType.LoadEmptyLeftSampleTray,
        TaskCardType.LoadEmptyRightSampleTray,
        TaskCardType.InventoryCarrierError,
        TaskCardType.InventoryCartridgeError,
        TaskCardType.BottleDrawerOpen,
        TaskCardType.BottleDrawerError,
        TaskCardType.UrgentLoading,
        TaskCardType.LiquidDrawerOpen,
        TaskCardType.CheckLiquidDrawer,
        TaskCardType.LiquidEmptyWaste,
        TaskCardType.LiquidRefillRinse,
        TaskCardType.SolidWasteDrawerOpen,
        TaskCardType.SolidWasteEmpty,
        TaskCardType.CheckWasteDrawer,
        TaskCardType.TemperatureOutOfRange,
        TaskCardType.HumidityOutOfRange,
        TaskCardType.SampleTubeError,
        TaskCardType.InstrumentSoftwareOffline,
        TaskCardType.SystemError,
        TaskCardType.CalibrationIssue,
        TaskCardType.ConnectionFailure,
        TaskCardType.ResultsIssue,
        TaskCardType.QCIssue,
        TaskCardType.RequestSampleIssue,
        TaskCardType.RequestReportResult,
        TaskCardType.ResultReviewPending
    ];

    private static readonly List<TaskCardType> _instrumentMandatory =
    [
        TaskCardType.SampleDrawerOpen,
        TaskCardType.SampleTubeError,
        TaskCardType.ResultReviewPending,
        TaskCardType.CalibrationIssue,
        TaskCardType.ReagentDrawerOpen,
        TaskCardType.ReagentDrawerError,
        TaskCardType.RemoveReagentsFromDrawer,
        TaskCardType.RemoveSamplesFromDrawer,
        TaskCardType.SampleTrayError,
        TaskCardType.SamplePartlyCompleted,
        TaskCardType.ReagentLoaderWithError,
        TaskCardType.MaintenanceCleaning,
        TaskCardType.MaintenanceManualCleaning,
        TaskCardType.NeedEmptyLoader,
        TaskCardType.LoadEmptySampleTray,
        TaskCardType.InventoryCarrierError,
        TaskCardType.InventoryCartridgeError,
        TaskCardType.LoadEmptyLeftSampleTray,
        TaskCardType.LoadEmptyRightSampleTray,
        TaskCardType.BottleDrawerOpen,
        TaskCardType.BottleDrawerError,
        TaskCardType.UrgentLoading,
        TaskCardType.LiquidDrawerOpen,
        TaskCardType.CheckLiquidDrawer,
        TaskCardType.LiquidEmptyWaste,
        TaskCardType.LiquidRefillRinse,
        TaskCardType.SolidWasteDrawerOpen,
        TaskCardType.SolidWasteEmpty,
        TaskCardType.CheckWasteDrawer,
        TaskCardType.TemperatureOutOfRange,
        TaskCardType.HumidityOutOfRange,
        TaskCardType.Error,
        TaskCardType.InstrumentSoftwareOffline,
        TaskCardType.ResultsIssue,
        TaskCardType.QCIssue
    ];

    private static readonly List<TaskCardType> _instrumentOptional =
    [
        TaskCardType.UpdatesAvailable
    ];

    private static readonly List<TaskCardType> _instrumentNotAllowed = [TaskCardType.SystemError, TaskCardType.ConnectionFailure, TaskCardType.RequestSampleIssue, TaskCardType.RequestReportResult];

    private static readonly List<TaskCardType> _controlsAllInstruments = [TaskCardType.SystemError, TaskCardType.ConnectionFailure, TaskCardType.RequestSampleIssue, TaskCardType.RequestReportResult];

    //should be the opposite of _taskCardsToRemoveWhenInstrumentIsDeleted
    private static readonly List<TaskCardType> _storeInDatabase =
    [
        TaskCardType.RequestSampleIssue,
        TaskCardType.RequestReportResult,
        TaskCardType.ResultReviewPending,
        TaskCardType.RequestReportResult,
        TaskCardType.ResultsIssue,
        TaskCardType.CalibrationIssue,
        TaskCardType.QCIssue
    ];

    public IReadOnlyList<TaskCardType> TaskCardsToRemoveWhenInstrumentIsDeleted()
    {
        return _taskCardsToRemoveWhenInstrumentIsDeleted;
    }

    public bool ValidateInstrumentNumberAndCardType(string? instrumentNumber, TaskCardType cardType)
    {
        //no instrument
        if (instrumentNumber == null)
        {
            return _instrumentOptional.Contains(cardType) || _instrumentNotAllowed.Contains(cardType);
        }

        //instrument
        return _instrumentOptional.Contains(cardType) || _instrumentMandatory.Contains(cardType);
    }

    public bool ShowOnLockScreen(TaskCardType cardType)
    {
        return _taskCardsToShowOnLockScreen.Contains(cardType);
    }

    public bool ControlsAllInstruments(TaskCardType cardType)
    {
        return _controlsAllInstruments.Contains(cardType);
    }

    public IReadOnlyList<TaskCardType> ControlsAllInstruments()
    {
        return _controlsAllInstruments;
    }

    public bool StoreInDatabase(TaskCardType cardType)
    {
        return _storeInDatabase.Contains(cardType);
    }
}
