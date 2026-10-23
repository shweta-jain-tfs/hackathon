namespace Pharos.Client.App.Sample.Models;

internal sealed class SampleTrayNotificationProvider : EntityNotificationProvider<SampleTray, string>
{
    public SampleTrayNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.SampleTray;
}
