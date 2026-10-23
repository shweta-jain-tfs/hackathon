using Pharos.Server.Application.Parameters;

namespace Pharos.Server.LoadList.Application.Events;

internal class MethodEnabledEventHandler(
    IInstrumentManager instrumentManager,
    ILoadListConfigurationInitializerService loadListConfigurationInitializerService,
    ILoadListDbContext dbContext) : INotificationHandler<MethodEnabledEvent>
{
    public async Task Handle(MethodEnabledEvent notification, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();

        foreach (var inst in instruments)
        {
            await loadListConfigurationInitializerService.InitializeForInstrument(inst.SerialNumber, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
