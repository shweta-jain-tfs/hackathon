
using MediatR;
using Pharos.Server.Application.Result;
using Pharos.Server.Instrument.Application.Commands.UpdateCurrentCalibrationInformation;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.SendCalibrationInformation;

internal record SendCalibrationInformationCommand(string InstrumentNumber, Guid? SessionId = null) : IStateSynchronizationCommand;

internal class SendCalibrationInformationCommandHandler(
    IMediator mediator,
    ICommunicationSessionManager sessionManager) : StateSynchronizationCommandHandlerBase<SendCalibrationInformationCommand>(sessionManager, CommunicationSynchronizationState.CalibrationInfoSent, mediator)

{
    protected override async Task DoHandle(SendCalibrationInformationCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var availableCalibrationInformation = await mediator.Send(new GetCalibrationValidDaysQuery(request.InstrumentNumber), cancellationToken);
        var currentCalibrationInformation = await session.Connection.SendCalibrationInformationAsync(Map(availableCalibrationInformation), cancellationToken);

        await mediator.Send(new UpdateCurrentCalibrationInformationCommand(request.InstrumentNumber, currentCalibrationInformation), cancellationToken);
    }

    private static AvailableCalibrationInformation Map(List<CalibrationValidDaysDto> list)
    {
        var calibrationInformation = new AvailableCalibrationInformation();

        foreach (var item in list)
        {
            var methodCalibrationInformation = new MethodCalibrationInformation
            {
                MethodName = item.MethodName
            };
            methodCalibrationInformation.ActiveLots.Add(Map(item.ConjugateLotNumber, item.ImmunoCapLotNumber));

            calibrationInformation.MethodCalibrationInformation.Add(methodCalibrationInformation);
        }

        return calibrationInformation;
    }

    private static LotInformation Map(string? conjugateLotNumber, string? immunoCapLotNumber)
    {
        return new LotInformation
        {
            ConjugateLotNumber = conjugateLotNumber,
            CarrierLotNumber = immunoCapLotNumber,
        };
    }
}