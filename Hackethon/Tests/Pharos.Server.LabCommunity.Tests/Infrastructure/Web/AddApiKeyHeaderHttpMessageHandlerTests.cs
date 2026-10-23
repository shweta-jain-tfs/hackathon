using Moq;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Infrastructure.Web;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Infrastructure.Web
{
    public static class AddApiKeyHeaderHttpMessageHandlerTests
    {
        public class TheSendAsyncMethod
        {
            private readonly Mock<IApiKeyService> _apiKeyService;
            private readonly AddApiKeyHeaderHttpMessageHandler _handler;
            private readonly CaptureHandler _capture;
            private readonly HttpRequestMessage _request;

            private const string _apiKeyHeaderName = "X-API-Key";

            private class CaptureHandler : HttpMessageHandler
            {
                public HttpRequestMessage? Request { get; private set; }

                protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    Request = request;

                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
                }
            }

            public TheSendAsyncMethod()
            {
                _apiKeyService = new Mock<IApiKeyService>();

                _capture = new CaptureHandler();
                _handler = new AddApiKeyHeaderHttpMessageHandler(_apiKeyService.Object)
                {
                    InnerHandler = _capture
                };

                _request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");
            }

            [Fact]
            public async Task DoesNotAddHeader_WhenApiKeyIsNullOrEmpty()
            {
                // Arrange
                _apiKeyService.Setup(s => s.GetApiKey()).ReturnsAsync((string?)null);

                using var invoker = new HttpMessageInvoker(_handler);

                // Act
                await invoker.SendAsync(_request, TestContext.Current.CancellationToken);

                // Assert
                Assert.False(_capture.Request!.Headers.Contains(_apiKeyHeaderName));
            }

            [Trait("ProductRequirement", "PR_533")]
            [Fact]
            public async Task AddsOrReplacesHeader_WhenApiKeyIsPresent()
            {
                // Arrange
                _apiKeyService.Setup(s => s.GetApiKey()).ReturnsAsync("new-api-key");

                using var invoker = new HttpMessageInvoker(_handler);
                // existing header should be removed and replaced
                _request.Headers.Add(_apiKeyHeaderName, "old-key");

                // Act
                await invoker.SendAsync(_request, TestContext.Current.CancellationToken);

                // Assert
                Assert.True(_capture.Request!.Headers.Contains(_apiKeyHeaderName));
                var values = _capture.Request.Headers.GetValues(_apiKeyHeaderName);
                Assert.Single(values);
                Assert.Contains("new-api-key", values);
            }
        }
    }
}