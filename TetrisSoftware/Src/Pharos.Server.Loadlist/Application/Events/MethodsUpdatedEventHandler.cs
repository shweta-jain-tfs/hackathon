using Pharos.Server.Application.Parameters;

namespace Pharos.Server.LoadList.Application.Events;

internal class MethodsUpdatedEventHandler(IInstrumentManager instrumentManager, ILoadListConfigurationInitializerService loadListConfigurationInitializerService, ILoadListDbContext dbContext) : INotificationHandler<MethodsUpdatedEvent>
{
    public async Task Handle(MethodsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();

        foreach (var inst in instruments)
        {
            await loadListConfigurationInitializerService.InitializeForInstrument(inst.SerialNumber, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}