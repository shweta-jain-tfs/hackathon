using Pharos.Client.App.Sample.Models;
using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.Sample.ViewModels;

public class RightSampleDrawerViewModel(
    IRightSampleDrawerDataSource rightSampleDrawerDataSource,
    ICurrentInstrumentNotificationProvider instrumentNotificationProvider,
    ISampleTrayPositionNotificationProvider sampleTrayPositionNotificationProvider,
    IEntityNotificationProvider<SampleTray, string> sampleTrayNotificationProvider,
    IAuthorizationService userInformationProvider)
    : SampleDrawerViewModelBase(rightSampleDrawerDataSource, instrumentNotificationProvider, sampleTrayPositionNotificationProvider, sampleTrayNotificationProvider, userInformationProvider)
{
    protected override void SubscribeToNotifications()
    {
        InstrumentNotificationProvider.RightSampleDrawerScanningStatusChanged += OnDrawerScanningStatusChanged;
        InstrumentNotificationProvider.RightSampleDrawerLockStateChanged += OnDrawerLockStateChanged;
        InstrumentNotificationProvider.RightSampleDrawerContentsChanged += OnSampleDrawerContentsChanged;
    }

    protected override void UnsubscribeFromNotifications()
    {
        InstrumentNotificationProvider.RightSampleDrawerScanningStatusChanged -= OnDrawerScanningStatusChanged;
        InstrumentNotificationProvider.RightSampleDrawerLockStateChanged -= OnDrawerLockStateChanged;
        InstrumentNotificationProvider.RightSampleDrawerContentsChanged -= OnSampleDrawerContentsChanged;
    }
}