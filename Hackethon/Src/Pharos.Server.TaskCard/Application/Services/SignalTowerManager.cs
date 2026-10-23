using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.TaskCards;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.TaskCard.Application.Services;

internal class SignalTowerManager : ISignalTowerManager
{
    private readonly IOptionsMonitor<TaskCardSettings> _taskCardSettings;

    private static readonly Dictionary<EnumSignalTowerControlSoundState, int> _soundSevereness = new()
    {
        { EnumSignalTowerControlSoundState.SoundContinuosOn, 10},
        { EnumSignalTowerControlSoundState.SoundShortSignal, 9},
        { EnumSignalTowerControlSoundState.SoundOff, 8},
        { EnumSignalTowerControlSoundState.SoundNotSet, 7}
    };

    private static readonly Dictionary<EnumSignalTowerLightState, int> _lightSevereness = new()
    {
        { EnumSignalTowerLightState.RedBlink, 10},
        { EnumSignalTowerLightState.Red, 9},
        { EnumSignalTowerLightState.Yellow, 8},
        { EnumSignalTowerLightState.Green, 7},
        { EnumSignalTowerLightState.Off, 6}
    };

    public SignalTowerManager(IOptionsMonitor<TaskCardSettings> taskCardSettings)
    {
        _taskCardSettings = taskCardSettings;
    }

    public ControlTower GetControlSignalTowerRequests(IReadOnlyList<Domain.TaskCard> taskCards, IReadOnlyList<TaskCardType> cardTypesThatControlsAllInstruments)
    {
        var items = new Dictionary<string, ControlSignalTowerRequest>();
        ControlSignalTowerRequest? requestForAllInstruments = null;

        foreach (var taskCard in taskCards)
        {
            if (!string.IsNullOrWhiteSpace(taskCard.InstrumentNumber))
            {
                if (items.TryGetValue(taskCard.InstrumentNumber, out var item))
                {
                    UpdateSignalTowerRequest(taskCard.CurrentCardState, ref item, _taskCardSettings.CurrentValue);
                }
                else
                {
                    item = CreateSignalTowerRequest(taskCard.CurrentCardState, _taskCardSettings.CurrentValue);
                    items.Add(taskCard.InstrumentNumber, item);
                }
            }

            if (cardTypesThatControlsAllInstruments.Contains(taskCard.CardType))
            {
                if (requestForAllInstruments is null)
                {
                    requestForAllInstruments = CreateSignalTowerRequest(taskCard.CurrentCardState, _taskCardSettings.CurrentValue);
                }
                else
                {
                    UpdateSignalTowerRequest(taskCard.CurrentCardState, ref requestForAllInstruments, _taskCardSettings.CurrentValue);
                }
            }
        }

        return new ControlTower(items, requestForAllInstruments);
    }

    public bool ShouldUpdateLightState(EnumSignalTowerLightState newState, EnumSignalTowerLightState currentState)
    {
        return _lightSevereness[newState] > _lightSevereness[currentState];
    }

    public bool ShouldUpdateSoundState(EnumSignalTowerControlSoundState newState, EnumSignalTowerControlSoundState currentState)
    {
        return _soundSevereness[newState] > _soundSevereness[currentState];
    }

    private void UpdateSignalTowerRequest(TaskCardState taskCardState, ref ControlSignalTowerRequest signalTowerRequest, TaskCardSettings setting)
    {
        var lightState = GetLightState(taskCardState);
        if (ShouldUpdateLightState(lightState, signalTowerRequest.SetLight))
        {
            signalTowerRequest.SetLight = lightState;
        }

        var soundState = GetSoundState(taskCardState, setting);
        if (ShouldUpdateSoundState(soundState, signalTowerRequest.SetSound))
        {
            signalTowerRequest.SetSound = soundState;
        }
    }

    private static EnumSignalTowerLightState GetLightState(TaskCardState taskCardState)
    {
        return taskCardState switch
        {
            TaskCardState.Hidden => EnumSignalTowerLightState.Off,
            TaskCardState.Information => EnumSignalTowerLightState.Off,
            TaskCardState.Warning => EnumSignalTowerLightState.Yellow,
            TaskCardState.Error => EnumSignalTowerLightState.Red,
            TaskCardState.FlashingRed => EnumSignalTowerLightState.RedBlink,
            _ => throw new ArgumentOutOfRangeException(nameof(taskCardState), taskCardState, null)
        };
    }

    private static ControlSignalTowerRequest CreateSignalTowerRequest(TaskCardState taskCardState, TaskCardSettings setting)
    {
        return new ControlSignalTowerRequest
               {
                   SetLight = GetLightState(taskCardState),
                   SetSound = GetSoundState(taskCardState, setting)
               };
    }

    private static EnumSignalTowerControlSoundState GetSoundState(TaskCardState taskCardState, TaskCardSettings setting)
    {
        if (!setting.Sound)
        {
            return EnumSignalTowerControlSoundState.SoundNotSet;
        }

        return taskCardState switch
        {
            TaskCardState.Hidden => EnumSignalTowerControlSoundState.SoundOff,
            TaskCardState.Information => EnumSignalTowerControlSoundState.SoundOff,
            TaskCardState.Warning => EnumSignalTowerControlSoundState.SoundShortSignal,
            TaskCardState.Error => EnumSignalTowerControlSoundState.SoundContinuosOn,
            TaskCardState.FlashingRed => EnumSignalTowerControlSoundState.SoundContinuosOn,
            _ => throw new ArgumentOutOfRangeException(nameof(taskCardState), taskCardState, null)
        };
    }
}