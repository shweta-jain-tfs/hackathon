namespace Pharos.Client.Common.Models.Services;

public sealed class ConnectivityHttpMessageHandler : DelegatingHandler
{
    private readonly IConnectionHealthService _connectionHealthService;

    public ConnectivityHttpMessageHandler(IConnectionHealthService connectionHealthService)
    {
        _connectionHealthService = connectionHealthService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            await _connectionHealthService.NotifyConnectionLostAsync(ex);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            await _connectionHealthService.NotifyConnectionLostAsync(ex);
            throw;
        }
    }
}
