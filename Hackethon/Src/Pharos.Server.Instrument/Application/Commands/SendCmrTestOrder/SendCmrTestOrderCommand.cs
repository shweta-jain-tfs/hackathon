using Google.Protobuf;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Instrument.Application.Commands.SendCmrTestOrder;

internal record SendCmrTestOrderCommand(string InstrumentNumber, byte[] Data) : IRequest<CmrTestOrderResultDto>, IInstrumentCommand;

internal class SendCmrTestOrderCommandHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentManager instrumentManager) : InstrumentCommandHandlerBase<SendCmrTestOrderCommand, CmrTestOrderResultDto>(sessionManager)
{
    protected override async Task<CmrTestOrderResultDto> Handle(SendCmrTestOrderCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(request.InstrumentNumber);

        if (instrument.State is not (InstrumentState.Standby or InstrumentState.AssayCmrFinished or InstrumentState.CmrPrepare))
        {
            return new CmrTestOrderResultDto { Status = CmrTestOrderStatus.NotAllowed };
        }

        var response = await session.Connection.SendCmrTestOrderAsync(new CmrTestOrderRequest
        {
            CustomMadeRunData = ByteString.CopyFrom(request.Data)
        }, cancellationToken);

        return response.ToCmrTestOrderResultDto();
    }
}
