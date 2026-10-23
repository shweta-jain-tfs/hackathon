using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.InstrumentManagement.Models.Instruments;

internal sealed class InstrumentListNotificationProvider : EntityNotificationProvider<Instrument>
{
    public InstrumentListNotificationProvider(IHubConnectionFactory hubConnectionFactory)
        : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Instrument;
}