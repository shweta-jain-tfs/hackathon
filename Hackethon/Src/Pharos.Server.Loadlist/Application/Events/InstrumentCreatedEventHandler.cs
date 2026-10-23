namespace Pharos.Server.LoadList.Application.Events;

internal class InstrumentCreatedEventHandler(ILoadListDbContext dbContext, ILoadListConfigurationInitializerService loadListConfigurationInitializerService) : INotificationHandler<InstrumentCreatedEvent>
{
    public async Task Handle(InstrumentCreatedEvent notification, CancellationToken cancellationToken)
    {
        await loadListConfigurationInitializerService.InitializeForInstrument(notification.InstrumentNumber, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}