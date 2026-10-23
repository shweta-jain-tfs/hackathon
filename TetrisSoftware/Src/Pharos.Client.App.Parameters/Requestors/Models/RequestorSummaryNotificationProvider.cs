using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Parameters.Requestors.Models;

internal sealed class RequestorSummaryNotificationProvider : EntityNotificationProvider<RequestorSummary>
{
    public RequestorSummaryNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Request;
}