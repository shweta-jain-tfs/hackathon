using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Common;

namespace Pharos.Server.Instrument.Application.Events;

internal class InstrumentStorageUpdatedEventHandler(IInstrumentManager instrumentManager, ICurrentInstrumentServerHub currentInstrumentHub) : INotificationHandler<InstrumentStorageUpdatedEvent>
{
    public async Task Handle(InstrumentStorageUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)instrumentManager.GetInstrument(notification.InstrumentNumber);

        var storage = notification.UpdatedStorage;

        var leftDrawerCurrent = instrument.DrawersScanState[DrawerType.Sample1];
        var leftDrawerUpdated = storage.SampleDrawers?.FirstOrDefault(x => x.Id == EnumSampleDrawer.Drawer1)?.ScanningOngoing ?? false;
        if (leftDrawerCurrent != leftDrawerUpdated)
        {
            instrument.DrawersScanState[DrawerType.Sample1] = leftDrawerUpdated;
            await currentInstrumentHub.LeftSampleDrawerScanningStatusChanged(instrument.Id, leftDrawerUpdated);
        }

        var rightDrawerCurrent = instrument.DrawersScanState[DrawerType.Sample2];
        var rightDrawerUpdated = storage.SampleDrawers?.FirstOrDefault(x => x.Id == EnumSampleDrawer.Drawer2)?.ScanningOngoing ?? false;
        if (rightDrawerCurrent != rightDrawerUpdated)
        {
            instrument.DrawersScanState[DrawerType.Sample2] = rightDrawerUpdated;
            await currentInstrumentHub.RightSampleDrawerScanningStatusChanged(instrument.Id, rightDrawerUpdated);
        }

        var reagentDrawerCurrent = instrument.DrawersScanState[DrawerType.Reagent];
        var reagentDrawerUpdated = storage.ReagentDrawer?.ScanningOngoing ?? false;
        if (reagentDrawerCurrent != reagentDrawerUpdated)
        {
            instrument.DrawersScanState[DrawerType.Reagent] = reagentDrawerUpdated;
            await currentInstrumentHub.ReagentDrawerScanningStatusChanged(instrument.Id, reagentDrawerUpdated);
        }
    }
}