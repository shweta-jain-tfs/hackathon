using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Instrument.Application.Commands.ControlSignalTower;

internal class MassControlSignalTowerCommandHandler : IRequestHandler<MassControlSignalTowerCommand>
{
    private readonly IInstrumentManager _instrumentManager;
    private readonly IPublisher _publisher;

    public MassControlSignalTowerCommandHandler(IInstrumentManager instrumentManager, IPublisher publisher)
    {
        _instrumentManager = instrumentManager;
        _publisher = publisher;
    }

    public async Task Handle(MassControlSignalTowerCommand request, CancellationToken cancellationToken)
    {
        Dictionary<string, ControlSignalTowerRequest> itemsToSend;

        // if something is to be sent to all instruments, set that request on all instruments
        if (request.AllInstruments is not null)
        {
            itemsToSend = _instrumentManager.GetInstruments()
                .Where(x => x.State != InstrumentState.Offline && x.State != InstrumentState.Synchronizing)
                .Select(x => (x.SerialNumber, GetSignalTowerRequestCopy(request.AllInstruments))).ToDictionary();
        }
        // otherwise, turn off the signal tower (might be overridden by instrument specific request)
        else
        {
            itemsToSend = _instrumentManager.GetInstruments()
                .Where(x => x.State != InstrumentState.Offline && x.State != InstrumentState.Synchronizing)
                .Select(x => (x.SerialNumber, new ControlSignalTowerRequest
                {
                    SetLight = EnumSignalTowerLightState.Off,
                    SetSound = EnumSignalTowerControlSoundState.SoundOff
                })).ToDictionary();
        }

        //if something is to be sent to specific instruments
        foreach (var item in request.InstrumentSpecific)
        {
            //the instrument is already in the list (added above), update the severity based on the specific request
            if (itemsToSend.TryGetValue(item.Key, out var currentState))
            {
                UpdateSeverityIfNeeded(ref currentState, item.Value);
            }
        }

        // Send to instruments
        foreach (var item in itemsToSend)
        {
            // We use Publish here because want to postpone the actual gRPC call
            // (we are likely in the middle of a gRPC handler and want to light up the tower after finding an error).
            await _publisher.Publish(new ControlSignalTowerCommand(item.Key, item.Value), cancellationToken);
        }
    }

    private static ControlSignalTowerRequest GetSignalTowerRequestCopy(ControlSignalTowerRequest requestAllInstruments)
    {
        return new ControlSignalTowerRequest
        {
            SetLight = requestAllInstruments.SetLight,
            SetSound = requestAllInstruments.SetSound
        };
    }

    private static void UpdateSeverityIfNeeded(ref ControlSignalTowerRequest instrumentSpecific, ControlSignalTowerRequest allInstrumentRequest)
    {
        if (instrumentSpecific.SetLight < allInstrumentRequest.SetLight)
        {
            instrumentSpecific.SetLight = allInstrumentRequest.SetLight;
        }

        if (instrumentSpecific.SetSound == EnumSignalTowerControlSoundState.SoundOff && allInstrumentRequest.SetSound == EnumSignalTowerControlSoundState.SoundShortSignal)
        {
            instrumentSpecific.SetSound = EnumSignalTowerControlSoundState.SoundShortSignal;
        }

        if ((instrumentSpecific.SetSound is EnumSignalTowerControlSoundState.SoundOff or EnumSignalTowerControlSoundState.SoundShortSignal) && allInstrumentRequest.SetSound == EnumSignalTowerControlSoundState.SoundContinuosOn)
        {
            instrumentSpecific.SetSound = EnumSignalTowerControlSoundState.SoundContinuosOn;
        }
    }
}