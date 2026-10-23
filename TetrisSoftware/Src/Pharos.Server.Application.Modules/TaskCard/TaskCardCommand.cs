using MediatR;

namespace Pharos.Server.Application.TaskCard;

public record TaskCardCommand(TaskCardType CardType, List<EscalationRule> Rules, TimeSpan? ExpiresAt = null, string? InstrumentNumber = null) : IRequest
{
    public TaskCardCommand(TaskCardType cardType, TaskCardState cardState, TimeSpan? expiresAt = null, string? instrumentNumber = null) : this(cardType, [new EscalationRule(cardState, TimeSpan.Zero)], expiresAt, instrumentNumber)
    {
    }

    public TaskCardCommand(TaskCardType cardType, TaskCardState cardState) : this(cardType, cardState, null, null)
    {
    }

    public TaskCardCommand(TaskCardType cardType, TaskCardState cardState, TimeSpan? expiresAt = null) : this(cardType, cardState, expiresAt, null)
    {
    }

    public TaskCardCommand(TaskCardType cardType, TaskCardState cardState, string? instrumentNumber = null) : this(cardType, cardState, null, instrumentNumber)
    {
    }

    public TaskCardCommand(TaskCardType cardType, List<EscalationRule> rules, TimeSpan? expiresAt = null) : this(cardType, rules, expiresAt, null)
    {
    }

    public TaskCardCommand(TaskCardType cardType, List<EscalationRule> rules, string? instrumentNumber = null) : this(cardType, rules, null, instrumentNumber)
    {
    }
}

public record EscalationRule(TaskCardState CardState, TimeSpan Trigger);

public enum TaskCardState
{
    Hidden,
    Information,
    Warning,
    Error,
    FlashingRed
}

public enum TaskCardType
{
    SampleDrawerOpen,
    Error,
    SampleTubeError,
    CalibrationIssue,
    ReagentDrawerOpen,
    ReagentDrawerError,
    RemoveReagentsFromDrawer,
    SampleTrayError,
    ReagentLoaderWithError,
    SamplePartlyCompleted,
    MaintenanceCleaning,
    MaintenanceManualCleaning,
    NeedEmptyLoader,
    LoadEmptySampleTray,
    LoadEmptyLeftSampleTray,
    LoadEmptyRightSampleTray,
    InventoryCarrierError,
    InventoryCartridgeError,
    BottleDrawerOpen,
    BottleDrawerError,
    UrgentLoading,
    LiquidDrawerOpen,
    LiquidEmptyWaste,
    LiquidRefillRinse,
    UpdatesAvailable,
    UpdateSuccessful,
    UpdateFailed,
    SolidWasteDrawerOpen,
    SolidWasteEmpty,
    TemperatureOutOfRange,
    HumidityOutOfRange,
    RemoveSamplesFromDrawer,
    CheckLiquidDrawer,
    CheckWasteDrawer,
    SystemError,
    InstrumentSoftwareOffline,
    ConnectionFailure,
    QCIssue,
    ResultsIssue,
    RequestSampleIssue,
    RequestReportResult,
    ResultReviewPending
}