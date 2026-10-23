using MediatR;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Reagents;
using Pharos.Server.Instrument.Application.Commands.UpdateArticles;

namespace Pharos.Server.Instrument.Application.Events;

internal class ReagentsSettingsChangedEventHandler(ISender sender) : INotificationHandler<SettingsChangedEvent<ReagentsSettings>>
{
    public Task Handle(SettingsChangedEvent<ReagentsSettings> notification, CancellationToken cancellationToken)
    {
        return sender.Send(new UpdateArticlesAllInstrumentsCommand(), cancellationToken);
    }
}
