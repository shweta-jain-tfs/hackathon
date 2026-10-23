using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.OrderNewCalCc;

internal class OrderNewCalCcCommandHandler(ICommunicationSessionManager sessionManager)
    : InstrumentCommandHandlerBase<OrderNewCalCcCommand>(sessionManager)
{
    protected override async Task Handle(OrderNewCalCcCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        await session.Connection.OrderNewCalCcAsync(new NewCalCcRequest
        {
            MethodName = request.MethodName,
            Type = request.IsCalibrator ? EnumCalibrationType.Calibrator : EnumCalibrationType.CurveControl,
            ConjugateLotNumber = request.ConjugateLot,
            CarrierLotNumber = request.CarrierLot,
            CalibratorLotNumber = request.CalibratorCurveControlLot,
        }, cancellationToken);
    }
}
