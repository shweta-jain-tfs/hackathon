using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Services;

public interface IAutomaticPowerSaveScheduler
{
    Task OnInstrumentStateChangedAsync(int instrumentId, InstrumentState state, CancellationToken cancellationToken);
    Task MarkLoggedInAsync(int instrumentId, CancellationToken cancellationToken);
    Task MarkLoggedOutAsync(int instrumentId, CancellationToken cancellationToken);
}
