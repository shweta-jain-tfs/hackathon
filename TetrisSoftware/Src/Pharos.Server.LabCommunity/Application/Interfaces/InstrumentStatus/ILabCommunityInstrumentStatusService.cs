namespace Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;

internal interface ILabCommunityInstrumentStatusService
{
    Task<bool> SendStatusAsync(CancellationToken cancellationToken = default);
}
