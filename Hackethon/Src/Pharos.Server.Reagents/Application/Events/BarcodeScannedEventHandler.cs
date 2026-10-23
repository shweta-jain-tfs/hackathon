using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Reagents.Application.Commands;

namespace Pharos.Server.Reagents.Application.Events;

internal class BarcodeScannedEventHandler(ICurrentInstrumentServerHub currentInstrumentHub, IReagentBarcodeService reagentBarcodeService,
    IInstrumentManager instrumentManager, IMediator mediator) : INotificationHandler<BarcodeScannedEvent>
{
    public async Task Handle(BarcodeScannedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Handled)
        {
            var instrument = instrumentManager.GetInstrument(notification.InstrumentNumber);
            var (articleType, barcodeFormat) = await reagentBarcodeService.DetectArticleType(notification.Barcode).ConfigureAwait(false);

            if (articleType is not ArticleType.None and
                (ArticleType.StopSolutionBottle or
                ArticleType.WashAdditiveBottle or
                ArticleType.WashConcentrateBottle))
            {
                if (barcodeFormat == BarcodeFormat.Primary)
                {
                    var lockState = instrument.DrawersLockState[DrawerType.BottledReagent].LockState;
                    if (lockState == LockState.Open)
                    {
                        await currentInstrumentHub.IncorrectBarcodeScannedForBottles(instrument.Id);
                    }
                }
                else
                {
                    var reagentInfo = await reagentBarcodeService.GetReagentInfo(articleType, secondaryBarcode: notification.Barcode);

                    if (reagentInfo != null)
                    {
                        await currentInstrumentHub.ReagentBarcodeScanned(instrument.Id, notification.Barcode);
                        await mediator.Send(new ProcessUpdateBottledReagentCommand(notification.Barcode, notification.InstrumentNumber, reagentInfo), cancellationToken);
                    }
                    else
                    {
                        throw new ArgumentException($"Reagent info not found for barcode: {notification.Barcode}");
                    }
                }
                notification.Handled = true;
            }
        }
    }
}