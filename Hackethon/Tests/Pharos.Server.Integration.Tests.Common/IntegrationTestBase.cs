using Microsoft.AspNetCore.SignalR.Client;

namespace Pharos.Server.Integration.Tests.Common;

public abstract class IntegrationTestBase(IntegrationTestsWaf factory) : DatabaseTestBase<IntegrationTestsWaf>(factory)
{
    protected static async Task WaitForHubResponse(Func<bool> waitUntilCondition)
    {
        var count = 0;
        while (!waitUntilCondition() && count < 10)
        {
            await Task.Delay(100);
            count++;
        }
    }

    protected async Task SetUpHubConnection(Action<int> action, string hubRoute, string methodName)
    {
        var connection = await HubConnectionHelper.SetupHubConnection(hubRoute, Factory);
        connection.On(methodName, action);
    }
}