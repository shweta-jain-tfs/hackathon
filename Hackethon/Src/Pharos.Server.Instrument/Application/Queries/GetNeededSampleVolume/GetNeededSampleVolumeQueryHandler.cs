using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Queries.GetNeededSampleVolume;

internal class GetNeededSampleVolumeQueryHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentManager manager) : InstrumentCommandHandlerBase<GetNeededSampleVolumeQuery, CalculateNeededSampleVolumeResponse?>(sessionManager)
{
    protected override bool ThrowIfNotAllowed => false;

    protected override CommunicationSession? GetCommunicationSession(IInstrumentCommand command)
    {
        if (string.IsNullOrEmpty(command.InstrumentNumber))
        {
            return GetRandomInstrumentSession();
        }
        return base.GetCommunicationSession(command);
    }

    protected override async Task<CalculateNeededSampleVolumeResponse?> Handle(GetNeededSampleVolumeQuery request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var message = new CalculateNeededSampleVolumeRequest();
        message.TestOrders.AddRange(request.TestOrders);
        return await session.Connection.CalculateNeededSampleVolumeAsync(message, cancellationToken);
    }

    private CommunicationSession? GetRandomInstrumentSession()
    {
        var notAllowedState = new List<InstrumentState>
        {
            InstrumentState.Synchronizing,
            InstrumentState.Limited,
            InstrumentState.Offline,
            InstrumentState.Shutdown
        };

        var potentialInstruments = manager.GetInstruments().Where(x => !notAllowedState.Contains(x.State)).ToList();

        while (potentialInstruments.Count > 0)
        {
            var instrument = TakeRandom(potentialInstruments);

            if (instrument == null)
            {
                return null;
            }

            var session = SessionManager.GetSession(instrument.SerialNumber);
            if (session == null)
            {
                potentialInstruments.Remove(instrument);
            }
            else
            {
                return session;
            }
        }

        return null;
    }

    private static IInstrument? TakeRandom(List<IInstrument> instruments)
    {
        if (instruments.Count == 0)
        {
            return null;
        }

        var index = System.Security.Cryptography.RandomNumberGenerator.GetInt32(instruments.Count);
        var instrument = instruments.ElementAt(index);
        return instrument;
    }
}
