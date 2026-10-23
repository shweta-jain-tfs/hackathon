using Pharos.Server.Application.TaskCard;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.TaskCard.Application.Interfaces;

internal interface ISignalTowerManager
{
    ControlTower GetControlSignalTowerRequests(IReadOnlyList<Domain.TaskCard> taskCards, IReadOnlyList<TaskCardType> cardTypesThatControlsAllInstruments);

    bool ShouldUpdateLightState(EnumSignalTowerLightState newState, EnumSignalTowerLightState currentState);

    bool ShouldUpdateSoundState(EnumSignalTowerControlSoundState newState, EnumSignalTowerControlSoundState currentState);

}