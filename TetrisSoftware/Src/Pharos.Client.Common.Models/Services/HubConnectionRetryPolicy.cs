using Microsoft.AspNetCore.SignalR.Client;

namespace Pharos.Client.Common.Models.Services;

internal class HubConnectionRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        // Retry delays: 2s, 5s, then 20s for all subsequent retries (never stops)
        var attempt = retryContext.PreviousRetryCount;
        return attempt switch
        {
            // We'll wait 2 seconds before the first retry to give some time to display
            // "connection lost" message and avoid flickering UI if the connection is restored quickly.
            0 => TimeSpan.FromSeconds(2),
            1 => TimeSpan.FromSeconds(5),
            _ => TimeSpan.FromSeconds(10),
        };
    }
}
