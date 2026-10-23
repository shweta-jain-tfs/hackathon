using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands.ProcessInstrumentState;

internal record ProcessInstrumentStateCommand(string InstrumentNumber, Guid? SessionId, InstrumentState Message) : IStateSynchronizationCommand;

internal class ProcessInstrumentStateCommandHandler(
    IInstrumentManager instrumentManager,
    ICommunicationSessionManager sessionManager,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<ProcessInstrumentStateCommand>(sessionManager, CommunicationSynchronizationState.InstrumentStateReceived, publisher)
{
    protected override async Task DoHandle(ProcessInstrumentStateCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        // if the session in not synchronized, the actual state update will be postponed
        if (session.IsSynchronized)
        {
            var instrument = (Domain.Instrument)instrumentManager.GetInstrument(session.InstrumentNumber);
            request.Message.MapStateToInstrument(instrument);
            session.CachedInstrumentState = null;

            await Publisher.Publish(new InstrumentStateChangedEvent(instrument.Id, instrument.State, instrument.TimeWhenNextStateReached), cancellationToken);
        }
        else
        {
            session.CachedInstrumentState = request.Message;
        }
    }
}
