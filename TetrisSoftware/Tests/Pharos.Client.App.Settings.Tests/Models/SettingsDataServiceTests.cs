using System.Net;
using System.Text;
using System.Text.Json;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.Models;

public class SettingsDataServiceTests
{
    [Fact]
    public async Task GetPageAsync_RelatedPageFromCachedRouteMapWasDeleted_RefreshesRouteMapAndRetriesRelatedPages()
    {
        using var handler = new StubHttpMessageHandler();
        handler.AddJsonResponse(CreatePage("Lis:LisChannels:6"));
        handler.AddJsonResponse(new Dictionary<string, string>
        {
            ["Lis/5"] = "Lis:LisChannels:5",
            ["Lis/6"] = "Lis:LisChannels:6"
        });
        handler.AddTextResponse(
            HttpStatusCode.InternalServerError,
            "Page with key [Lis:LisChannels:5] not found");
        handler.AddJsonResponse(new Dictionary<string, string>
        {
            ["Lis/6"] = "Lis:LisChannels:6"
        });
        handler.AddJsonResponse(CreatePage("Lis:LisChannels:6"));

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        var sut = new SettingsDataService(httpClient);

        var result = await sut.GetPageAsync("Lis", "Lis:LisChannels:6");

        result.Key.Should().Be("Lis:LisChannels:6");
        sut.KeyItemsPages.Should().ContainSingle(page => page.Key == "Lis:LisChannels:6");
        handler.RequestUrls.Should().Equal(
            "api/settings/Lis%3ALisChannels%3A6?rootPageLabel=Lis",
            "pageKeyMap",
            "api/settings/Lis%3ALisChannels%3A5?rootPageLabel=Lis",
            "pageKeyMap",
            "api/settings/Lis%3ALisChannels%3A6?rootPageLabel=Lis");
    }

    private static Page CreatePage(string key)
    {
        return new Page
        {
            Key = key,
            Label = key,
            Url = key.Replace(':', '/'),
            Header = key,
            Children = [],
            Dependency = [],
            Permissions = []
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = [];

        public List<string> RequestUrls { get; } = [];

        public void AddJsonResponse(object response)
        {
            _responses.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(response),
                    Encoding.UTF8,
                    "application/json")
            });
        }

        public void AddTextResponse(HttpStatusCode statusCode, string response)
        {
            _responses.Enqueue(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response)
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUrls.Add(request.RequestUri?.PathAndQuery.TrimStart('/') ?? string.Empty);

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No stubbed HTTP response was configured.");
            }

            return Task.FromResult(_responses.Dequeue()());
        }
    }
}
