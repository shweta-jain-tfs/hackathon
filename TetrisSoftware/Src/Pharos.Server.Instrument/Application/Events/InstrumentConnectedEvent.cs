using MediatR;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.SendCalibrationInformation;
using Pharos.Server.Instrument.Application.Commands.SendQcScheduling;
using Pharos.Server.Instrument.Application.Commands.UpdateArticles;
using Pharos.Server.Instrument.Application.Commands.UpdateLimits;
using Pharos.Server.Instrument.Application.Commands.UpdateMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateMethodStatus;
using Pharos.Server.Instrument.Application.Commands.UpdateSettings;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Events;

/// <summary>
/// This event is published every time after Pharos has processed the <see cref="ModuleInformation"/> sent by ISW.
/// </summary>
/// <remarks>
/// At this point, the instrument is registered in the database, and we can start state synchronization.
/// </remarks>
internal record InstrumentConnectedEvent(Guid SessionId, string InstrumentNumber, EnumCompatibilityStatus CompatibilityStatus) : INotification;

internal class InstrumentConnectedEventHandler(ISender sender) : INotificationHandler<InstrumentConnectedEvent>
{
    public async Task Handle(InstrumentConnectedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.CompatibilityStatus != EnumCompatibilityStatus.FullyCompatible)
        {
            return;
        }

        await sender.Send(new UpdateLimitsCommand(notification.InstrumentNumber, notification.SessionId), cancellationToken);
        await sender.Send(new UpdateMethodsCommand(notification.InstrumentNumber, notification.SessionId), cancellationToken);
        await sender.Send(new UpdateArticlesCommand(notification.InstrumentNumber, notification.SessionId), cancellationToken);
        await sender.Send(new SendActiveMethodsCommand(notification.InstrumentNumber, notification.SessionId), cancellationToken);
        await sender.Send(new SendQcSchedulingCommand(notification.InstrumentNumber, notification.SessionId), cancellationToken);
        await sender.Send(new SendCalibrationInformationCommand(notification.InstrumentNumber), cancellationToken);
        await sender.Send(new UpdateSettingsCommand(notification.InstrumentNumber, notification.SessionId), cancellationToken);
        await sender.Send(new UpdateMethodStatusAllCommand(notification.InstrumentNumber), cancellationToken);
    }
}