using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Parameters.Reflex.Models;

public interface IReflexDetailNotificationProvider : IDetailsNotificationProvider<int>;

public class ReflexDetailNotificationProvider : DetailsPageNotificationProvider<int>, IReflexDetailNotificationProvider
{
    public ReflexDetailNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Reflex;
}