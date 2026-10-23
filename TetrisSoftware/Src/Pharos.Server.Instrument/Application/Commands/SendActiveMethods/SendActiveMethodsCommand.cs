using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.SendActiveMethods;

internal record SendActiveMethodsCommand(string InstrumentNumber, Guid? SessionId = null) : IStateSynchronizationCommand;

internal class SendActiveMethodsCommandHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentDbContext dbContext,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<SendActiveMethodsCommand>(sessionManager, CommunicationSynchronizationState.ActiveMethodsSent, publisher)
{
    protected override async Task DoHandle(SendActiveMethodsCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var activeInstrumentMethods = await dbContext.InstrumentMethods
            .Where(x => x.Instrument.SerialNumber == request.InstrumentNumber && x.IsActive)
            .Select(x => x.MethodName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var methodsRequest = new ActiveMethodsRequest();
        methodsRequest.MethodNames.AddRange(activeInstrumentMethods);

        // TODO: How do we handle instrument messages when instrument is offline?
        await session.Connection.SendActiveMethodsAsync(methodsRequest, cancellationToken);
    }
}
