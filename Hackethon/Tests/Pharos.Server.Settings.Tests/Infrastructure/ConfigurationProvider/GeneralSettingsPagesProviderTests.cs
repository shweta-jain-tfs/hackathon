using Microsoft.Extensions.Localization;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class GeneralSettingsPagesProviderTests : IDisposable
{
    private readonly ISettingsStringLocalizer _localizer;

    public GeneralSettingsPagesProviderTests()
    {
        SettingsPagesProvider.Initialize(new[] { typeof(GeneralSettings).Assembly });

        var mockLocalizer = new Mock<ISettingsStringLocalizer>();
        mockLocalizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));

        _localizer = mockLocalizer.Object;
    }

    public void Dispose()
    {
        BaseSettingsPagesProvider.Initialize([]);
    }

    [Fact]
    public void GeneralSettings_ShouldContainPharosMarketGroup_AsChoice_WithWorldwideDefault()
    {
        var page = SettingsPagesProvider
            .GetAllPages(_localizer, _ => new[] { typeof(GeneralSettings) })
            .Single()
            .Value;

        page.Children.Should().HaveCount(2);
        page.Children[0].Should().BeOfType<Group>();
        page.Children[0].Key.Should().Be("General:RegionalSettings");
        page.Children[1].Should().BeOfType<Group>();
        page.Children[1].Key.Should().Be("General:ProductsSettings");

        var marketGroup = page.Children[1].As<Group>();
        var marketOption = marketGroup.Children.Single().As<Option>();

        marketOption.Key.Should().Be("General:ProductsSettings:Market");
        marketOption.Type.Should().Be(OptionType.Choice);
        marketOption.Value.Should().Be(nameof(ProductsMarket.Worldwide));
        marketOption.DynamicDropdownProviderType.Should().BeNull();
    }

    [Fact]
    public void GeneralSettings_ProductsSettings_ShouldRequireEditSystemSettingsPermission()
    {
        var page = SettingsPagesProvider
            .GetAllPages(_localizer, _ => new[] { typeof(GeneralSettings) })
            .Single()
            .Value;

        page.Children.Should().HaveCount(2);

        var productsGroup = page.Children[1].As<Group>();
        productsGroup.Key.Should().Be("General:ProductsSettings");
        productsGroup.Permissions.Should().Contain(Permissions.EditSystemSettings);
        productsGroup.Permissions.Should().HaveCount(1);
    }
}
