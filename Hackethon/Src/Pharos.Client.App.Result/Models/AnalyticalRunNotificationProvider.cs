using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Result.Models;
internal sealed class AnalyticalRunNotificationProvider(IHubConnectionFactory hubConnectionFactory) : EntityNotificationProvider<AnalyticalRun>(hubConnectionFactory)
{
    protected override string HubRoute => HubRoutes.Run;
}
