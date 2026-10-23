using Pharos.Server.Application.Parameters;

namespace Pharos.Server.LoadList.Application.Events;

internal class QcEnableDisableEventHandler(IInstrumentManager instrumentManager, ILoadListConfigurationInitializerService loadListConfigurationInitializerService, ILoadListDbContext dbContext) : INotificationHandler<QcEnableDisableEvent>
{
    public async Task Handle(QcEnableDisableEvent notification, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();

        foreach (var inst in instruments)
        {
            await loadListConfigurationInitializerService.InitializeQcForInstrument(inst.SerialNumber, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}