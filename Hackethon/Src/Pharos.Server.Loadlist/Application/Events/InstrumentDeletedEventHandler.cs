using Microsoft.EntityFrameworkCore;

namespace Pharos.Server.LoadList.Application.Events;

internal class InstrumentDeletedEventHandler(ILoadListDbContext dbContext, ILoadListService loadListService) : INotificationHandler<InstrumentDeletedEvent>
{
    public async Task Handle(InstrumentDeletedEvent notification, CancellationToken cancellationToken)
    {
        // Delete all load list configurations for this instrument
        await dbContext.LoadListConfigurations
            .Where(x => x.InstrumentSerial == notification.InstrumentNumber)
            .ExecuteDeleteAsync(cancellationToken);

        // Delete all last loaded reagents for this instrument
        await dbContext.LastLoadedReagents
            .Where(x => x.InstrumentNumber == notification.InstrumentNumber)
            .ExecuteDeleteAsync(cancellationToken);

        // Delete all instruments from the in memory representation of the load list
        loadListService.DeleteInstrument(notification.InstrumentNumber);
    }
}