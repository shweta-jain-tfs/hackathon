using MediatR;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Localization;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands;

internal abstract class InstrumentCommandHandlerBase(ICommunicationSessionManager sessionManager)
{
    /// <summary>
    /// The value indicating whether this request can be sent to ISW that has FilesApiOnly compatibility level.
    /// </summary>
    protected virtual bool AllowedInLimitedMode => false;

    /// <summary>
    /// The value indicating whether this request can be sent to ISW if the session has not been synchronized yet.
    /// </summary>
    protected virtual bool AllowedIfNotSynchronized => false;

    /// <summary>
    /// The value indicating if <see cref="NotAllowedException"/> must be thrown if request cannot be sent.
    /// </summary>
    protected virtual bool ThrowIfNotAllowed => true;

    protected ICommunicationSessionManager SessionManager => sessionManager;

    protected virtual CommunicationSession? GetCommunicationSession(IInstrumentCommand command)
    {
        var session = SessionManager.GetSession(command.InstrumentNumber);
        if (session == null)
        {
            if (ThrowIfNotAllowed)
            {
                throw new NotAllowedException(Translations.ErrorSendingRequestToDisconnectedInstrument);
            }
        }
        else if (session.CompatibilityStatus == EnumCompatibilityStatus.FullyCompatible)
        {
            if (!session.IsSynchronized && !AllowedIfNotSynchronized)
            {
                if (ThrowIfNotAllowed)
                {
                    throw new NotAllowedException($"Sending {command.GetType().Name} request is not allowed until the session state is synchronized.");
                }

                return null;
            }
        }
        else if (session.CompatibilityStatus == EnumCompatibilityStatus.Incompatible)
        {
            // should never happen, but if it does, we want to know about it
            throw new InvalidOperationException("Incompatible instrument should not have a session created.");
        }
        else if (session.CompatibilityStatus == EnumCompatibilityStatus.FilesApiOnly && !AllowedInLimitedMode)
        {
            if (ThrowIfNotAllowed)
            {
                throw new NotAllowedException(Translations.ErrorSendingRequestLimitedCompatibility);
            }

            return null;
        }

        return session;
    }
}

internal abstract class InstrumentCommandHandlerBase<TInstrumentCommand>(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase(sessionManager), IRequestHandler<TInstrumentCommand> where TInstrumentCommand : IInstrumentCommand, IRequest
{
    public virtual async Task Handle(TInstrumentCommand request, CancellationToken cancellationToken)
    {
        var session = GetCommunicationSession(request);
        if (session != null)
        {
            await Handle(request, session, cancellationToken);
        }
    }

    protected abstract Task Handle(TInstrumentCommand request, CommunicationSession session, CancellationToken cancellationToken);
}

internal abstract class InstrumentCommandHandlerBase<TInstrumentCommand, TResponse>(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase(sessionManager), IRequestHandler<TInstrumentCommand, TResponse> where TInstrumentCommand : IInstrumentCommand, IRequest<TResponse>
{
    public virtual async Task<TResponse> Handle(TInstrumentCommand request, CancellationToken cancellationToken)
    {
        var session = GetCommunicationSession(request);
        if (session != null)
        {
            return await Handle(request, session, cancellationToken);
        }

        return GetDefaultResponse(request);
    }

    protected virtual TResponse GetDefaultResponse(TInstrumentCommand request)
    {
        return default!;
    }

    protected abstract Task<TResponse> Handle(TInstrumentCommand request, CommunicationSession session, CancellationToken cancellationToken);
}