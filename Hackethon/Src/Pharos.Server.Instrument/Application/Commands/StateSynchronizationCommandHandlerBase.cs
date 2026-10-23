using MediatR;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Commands;

internal abstract class StateSynchronizationCommandHandlerBase<TRequest>(
    ICommunicationSessionManager sessionManager,
    CommunicationSynchronizationState flag,
    IPublisher publisher) : InstrumentCommandHandlerBase<TRequest>(sessionManager) where TRequest : IStateSynchronizationCommand
{
    protected override bool AllowedIfNotSynchronized => true;

    protected IPublisher Publisher => publisher;

    protected abstract Task DoHandle(TRequest request, CommunicationSession session, CancellationToken cancellationToken);

    protected sealed override async Task Handle(TRequest request, CommunicationSession session, CancellationToken cancellationToken)
    {
        if (request.SessionId != null && session.SessionId != request.SessionId.Value)
        {
            if (ThrowIfNotAllowed)
            {
                throw new NotAllowedException("The instrument session has been reset during synchronization. The request must be discarded");
            }
            return;
        }

        await DoHandle(request, session, cancellationToken);

        if (session.SetSynchronizationFlag(flag))
        {
            // Notify that the session is fully synchronized
            await Publisher.Publish(new CommunicationSessionSynchronizedEvent(session), cancellationToken);
        }
    }
}