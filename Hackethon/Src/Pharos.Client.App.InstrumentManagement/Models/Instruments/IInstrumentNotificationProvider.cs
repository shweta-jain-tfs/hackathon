using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

public interface IInstrumentNotificationProvider : INotificationProvider
{
    event Action<int, int, bool, DateTimeOffset?> AssaySchedulerChanged;
    event Action<int, int, AssaySchedulerDay, bool> AssaySchedulerDayChanged;
    event Action<int, DateTimeOffset?, AssaySchedulerNextRunType> AssaySchedulerNextRunChanged;
    event Action<int, InstrumentState> InstrumentStateChanged;
    event Action<int, DateTimeOffset?> TimeWhenNextStateReachedChanged;
    event Action<int> InstrumentTechnologiesChanged;
    event EventHandler NumberOfTestsUpdated;
    event EventHandler SignalTowerStateChanged;
}
