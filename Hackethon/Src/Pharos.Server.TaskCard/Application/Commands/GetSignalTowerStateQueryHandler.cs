using MediatR;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.TaskCard.Application.Commands;

internal class GetSignalTowerStateQueryHandler(ITaskCardService taskCards, ISignalTowerManager signalTower) : IRequestHandler<GetSignalTowerStateQuery, ControlSignalTowerRequest>
{
    public Task<ControlSignalTowerRequest> Handle(GetSignalTowerStateQuery request, CancellationToken cancellationToken)
    {
        var overallState = taskCards.GetControlSignalTowerRequests();

        var instrumentState = overallState.AllInstruments
                              ?? new ControlSignalTowerRequest
                              {
                                  SetLight = EnumSignalTowerLightState.Off,
                                  SetSound = EnumSignalTowerControlSoundState.SoundOff
                              };

        if (overallState.InstrumentSpecific.TryGetValue(request.InstrumentNumber, out var value))
        {
            if (signalTower.ShouldUpdateLightState(value.SetLight, instrumentState.SetLight))
            {
                instrumentState.SetLight = value.SetLight;
            }

            if (signalTower.ShouldUpdateSoundState(value.SetSound, instrumentState.SetSound))
            {
                instrumentState.SetSound = value.SetSound;
            }
        }

        return Task.FromResult(instrumentState);
    }
}