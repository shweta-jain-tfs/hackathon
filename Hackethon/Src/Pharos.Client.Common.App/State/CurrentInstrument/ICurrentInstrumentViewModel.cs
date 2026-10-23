namespace Pharos.Client.Common.App.State.CurrentInstrument;

public interface ICurrentInstrumentViewModel : IDisposable
{
    string Name { get; }
    bool Enabled { get; }
    LockState HoodLockState { get; }
    bool IsHoodUnlockInProgress { get; }
    InstrumentState State { get; }
    public bool IsStateChangeRequested { get; set; }
    int NumberOfTaskCards { get; }
    int NumberOfTests { get; }
    string SerialNumber { get; }
    SignalTowerViewModel SignalTower { get; }
    DateTimeOffset? TimeWhenNextStateReached { get; }
    AssaySchedulerNextRunType AssaySchedulerNextRunType { get; }
    DateTimeOffset? AssaySchedulerNextRun { get; }
    bool PlaySound { get; set; }
    TaskCardState HighestUrgencyLevel { get; }
    bool MaintenanceNeeded { get; }

    Task LoadData();

    event Action OnCurrentInstrumentChange;
    event Action OnProcessStatusChanged;
    event Action OnSignalTowerChange;
    event Action OnTaskCardChange;
    event Action<List<int>>? OnFatalErrorChanged;
    event Action OnCurrentInstrumentSampleDrawerOpen;
    event Action OnCurrentInstrumentReagentDrawerOpen;
    event Action OnCurrentInstrumentBottleDrawerOpen;
    event Action OnCurrentInstrumentLiquidDrawerOpen;
    event Action OnCurrentInstrumentSolidWasteDrawerOpen;
    event Action<TaskCardType, TaskCardState> OnTaskCardCreatedOrChanged;
    public event Action<string>? OnCurrentInstrumentUnknownBarcodeScanned;
    Task MuteSound();
}
