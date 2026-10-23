using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.Sample.ViewModels;

public class LeftSampleDrawerViewModel(
    ILeftSampleDrawerDataSource leftSampleDrawerDataSource,
    ICurrentInstrumentNotificationProvider notificationProvider,
    ISampleTrayPositionNotificationProvider sampleTrayPositionNotificationProvider,
    IEntityNotificationProvider<SampleTray, string> sampleTrayNotificationProvider,
    IAuthorizationService userInformationProvider)
    : SampleDrawerViewModelBase(leftSampleDrawerDataSource, notificationProvider, sampleTrayPositionNotificationProvider, sampleTrayNotificationProvider, userInformationProvider)
{
    protected override void SubscribeToNotifications()
    {
        InstrumentNotificationProvider.LeftSampleDrawerScanningStatusChanged += OnDrawerScanningStatusChanged;
        InstrumentNotificationProvider.LeftSampleDrawerLockStateChanged += OnDrawerLockStateChanged;
        InstrumentNotificationProvider.LeftSampleDrawerContentsChanged += OnSampleDrawerContentsChanged;
    }

    protected override void UnsubscribeFromNotifications()
    {
        InstrumentNotificationProvider.LeftSampleDrawerScanningStatusChanged -= OnDrawerScanningStatusChanged;
        InstrumentNotificationProvider.LeftSampleDrawerLockStateChanged -= OnDrawerLockStateChanged;
        InstrumentNotificationProvider.LeftSampleDrawerContentsChanged -= OnSampleDrawerContentsChanged;
    }
}