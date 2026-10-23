using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;
using Tetris.Protocol.Instrument.Control;
using Tetris.Protocol.Pharos;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Instrument.Application.Events;

internal class UserLoggedInEventHandler(
    IDrawerAuthorizationService drawerAuthorizationService,
    IInstrumentManager instrumentManager,
    ICommunicationSessionManager sessionManager,
    IAutomaticPowerSaveScheduler automaticPowerSaveScheduler,
    ILogger<UserLoggedInEventHandler> logger) : INotificationHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        if (notification.InstrumentNumber == null)
        {
            return;
        }

        var instrument = instrumentManager.GetInstrument(notification.InstrumentNumber);

        // Mark instrument as logged in. It will cancel any pending automatic power save.
        await automaticPowerSaveScheduler.MarkLoggedInAsync(instrument.Id, cancellationToken);

        if (notification.UserId.HasValue)
        {
            await drawerAuthorizationService.SendUnlockIfWaiting(notification.UserId.Value, notification.InstrumentNumber);
        }

        // Wake instrument: send Standby request if currently in Standby or PowerSaving.
        // (Instrument may be transitioning to PowerSaving while still showing Standby state, then we
        // abort that action with sending Standby again).
        if (instrument.State is InstrumentState.Standby or InstrumentState.PowerSaving)
        {
            var session = sessionManager.GetSession(notification.InstrumentNumber);
            if (session is { CompatibilityStatus: EnumCompatibilityStatus.FullyCompatible, IsSynchronized: true })
            {
                var request = new ControlInstrumentStateRequest
                {
                    OrderedState = EnumOrderedInstrumentState.Standby,
                    OrderedStateFinishOption = EnumStateFinishOptions.UnspecifiedStateFinishOption,
                    OrderedStateRinseOption = EnumStateRinseOptions.UnspecifiedStateRinseOption
                };

                var response = await session.Connection.ControlInstrumentStateAsync(request, cancellationToken);

                if (response.Response != EnumInstrumentStateResponse.Ok)
                {
                    logger.LogWarning("Request to set instrument {InstrumentId} ({SerialNumber}) to Standby returned {Response}", instrument.Id, instrument.SerialNumber, response.Response);
                }
            }
        }
    }
}
