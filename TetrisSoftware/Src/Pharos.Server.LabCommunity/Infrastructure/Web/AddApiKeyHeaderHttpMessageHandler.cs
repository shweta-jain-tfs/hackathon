using Pharos.Server.LabCommunity.Application.Interfaces;

namespace Pharos.Server.LabCommunity.Infrastructure.Web;

internal class AddApiKeyHeaderHttpMessageHandler : DelegatingHandler
{
    private readonly IApiKeyService _apiKeyService;

    public AddApiKeyHeaderHttpMessageHandler(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyService.GetApiKey();

        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Remove("X-API-Key");
            request.Headers.Add("X-API-Key", apiKey);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
