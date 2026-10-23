namespace Pharos.Common.Web.Components.Loader
{
    public class DisplayLoaderAutomaticallyHttpMessageHandler(LoaderService loaderService) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var callId = Guid.NewGuid();
            try
            {
                loaderService.Show(callId);

                return await base.SendAsync(request, cancellationToken);
            }
            finally
            {
                loaderService.Hide(callId);
            }
        }
    }
}