using Pharos.Contracts.Notifications;

namespace Pharos.Client.App.Result.Models;

internal sealed class TestResultNotificationProvider : EntityNotificationProvider<AnalyticalRunTestResult>
{
    public TestResultNotificationProvider(IHubConnectionFactory hubConnectionFactory) : base(hubConnectionFactory)
    {
    }

    protected override string HubRoute => HubRoutes.Result;
}