using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Parameters.Requestors.Models;

public interface IRequestorDetailsNotificationProvider : IDetailsNotificationProvider<int>;

internal sealed class RequestorDetailsNotificationProvider : DetailsPageNotificationProvider<int>, IRequestorDetailsNotificationProvider
{
    public RequestorDetailsNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Request;
}