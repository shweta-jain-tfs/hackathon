using System.Net;

namespace Pharos.Client.Services;

internal class AuthorizationHttpMessageHandler(NavigationManager navigationManager, ILogger<AuthorizationHttpMessageHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sending HTTP request: {RequestUri}", request.RequestUri);

        //TODO: Needs more work. It can go in to an infinite reload under right circumstances.
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Unauthorized response received, refreshing the page.");
            navigationManager.Refresh(forceReload: true);
        }

        // Refresh on 403 removed for now to avoid repeated reloads
        //if (response.StatusCode == HttpStatusCode.Forbidden)
        //{
        //    // todo: show "you're not authorized" message box before reloading
        //    logger.LogWarning("Forbidden response received, refreshing the page.");
        //    navigationManager.Refresh(forceReload: true);
        //}

        return response;
    }
}
