using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Queries;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Queries;

public class GetPageKeyMapQueryHandlerTests : IDisposable
{
    private readonly Mock<ISettingsPermissionHelper> _permissionHelperMock = new();
    private readonly Mock<ISettingsStringLocalizer> _localizerMock = new();
    private readonly Mock<ISettingsPagesLoader> _loaderMock = new();

    public GetPageKeyMapQueryHandlerTests()
    {
        _localizerMock.Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key, resourceNotFound: true));
    }

    public void Dispose()
    {
        BaseSettingsPagesProvider.Initialize([]);
    }

    [Fact]
    public async Task Handle_ShouldMapGroupUrlToContainingPageKey()
    {
        SettingsPagesProvider.Initialize([GetType().Assembly], _ => [typeof(PageKeyMapRoot)]);

        var page = new Page
        {
            Key = "PageKeyMapRoot",
            Label = "Page key map",
            Url = "Page key map",
            Children =
            [
                new Group
                {
                    Key = "PageKeyMapRoot:General",
                    Label = "General",
                    Url = "Page key map/General",
                    Children =
                    [
                        new Option
                        {
                            Key = "PageKeyMapRoot:General:Enabled",
                            Label = "Enabled"
                        }
                    ]
                }
            ]
        };

        _permissionHelperMock
            .Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);
        _loaderMock
            .Setup(x => x.LoadAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Page>
            {
                [page.Key] = page
            });

        var handler = new GetPageKeyMapQueryHandler(
            _permissionHelperMock.Object,
            _localizerMock.Object,
            _loaderMock.Object);

        var result = await handler.Handle(new GetPageKeyMapQuery(), TestContext.Current.CancellationToken);

        result["Page key map"].Should().Be("PageKeyMapRoot");
        result["Page key map/General"].Should().Be("PageKeyMapRoot");
    }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("Page key map")]
internal class PageKeyMapRoot
{
    [SettingsGroup]
    [DisplayNameLocal("General")]
    public PageKeyMapGroup General { get; set; } = new();
}

internal class PageKeyMapGroup
{
    [DisplayNameLocal("Enabled")]
    public bool Enabled { get; set; }
}