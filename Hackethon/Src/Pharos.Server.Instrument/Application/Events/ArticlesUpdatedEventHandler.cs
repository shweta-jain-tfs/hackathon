using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Instrument.Application.Commands.UpdateArticles;

namespace Pharos.Server.Instrument.Application.Events;

internal class ArticlesUpdatedEventHandler(ISender sender) : INotificationHandler<ArticlesUpdatedEvent>
{
    public Task Handle(ArticlesUpdatedEvent notification, CancellationToken cancellationToken)
    {
        return sender.Send(new UpdateArticlesAllInstrumentsCommand(), cancellationToken);
    }
}
