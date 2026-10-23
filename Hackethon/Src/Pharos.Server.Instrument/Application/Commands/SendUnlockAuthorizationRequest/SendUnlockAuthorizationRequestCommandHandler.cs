using MediatR;
using Pharos.Server.Application.Sample;

namespace Pharos.Server.Instrument.Application.Commands.SendUnlockAuthorizationRequest;

internal class SendUnlockAuthorizationRequestCommandHandler : IRequestHandler<SendUnlockAuthorizationRequestCommand>
{
    private readonly IPublisher _publisher;

    public SendUnlockAuthorizationRequestCommandHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(SendUnlockAuthorizationRequestCommand request, CancellationToken cancellationToken)
    {
        await _publisher.Publish(new SendDrawerUnlockAuthorizationRequestEvent(request.InstrumentNumber, request.CompartmentWithLock), cancellationToken);
    }
}
