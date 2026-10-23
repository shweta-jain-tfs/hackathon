using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.AssayScheduler;

internal sealed class AssaySchedulerListNotificationProvider(IHubConnectionFactory hubConnectionFactory)
    : EntityNotificationProvider<Common.Models.AssayScheduler>(hubConnectionFactory)
{
    protected override string HubRoute => HubRoutes.Instrument;
}