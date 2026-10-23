using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;


namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class OptionTests : IDisposable
{
    private readonly ISettingsStringLocalizer _localizer;

    public OptionTests()
    {
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
    public void OptionWithDisplayModeRadioButtonsAttribute_ShouldBeSetByPagesProvider()
    {
        SettingsPagesProvider.Initialize(new[] { typeof(PageWithRadioButtonsOption).Assembly });

        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageWithRadioButtonsOption) }).FirstOrDefault();

        var page = pages.Value;
        Assert.NotNull(page);

        var option = page.Children.OfType<Option>().FirstOrDefault(o => o.Key == "PageWithRadioButtonsOption:RadioOption");
        Assert.NotNull(option);
        Assert.True(option.IsDisplayModeRadioButtons);
    }

    [Fact]
    public void OptionWithDisallowedCharactersAttribute_ShouldExposePattern()
    {
        SettingsPagesProvider.Initialize(new[] { typeof(PageWithDisallowedCharactersOption).Assembly });

        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageWithDisallowedCharactersOption) });

        var option = pages.Single().Value.Children.OfType<Option>().Single();
        option.DisallowedCharactersPattern.Should().Be(@"[<>]");
    }

    [Fact]
    public void OptionDeepCopy_ShouldCopyDisallowedCharactersPattern()
    {
        var option = new Option
        {
            Key = "Root:Name",
            DisallowedCharactersPattern = @"[<>]"
        };

        var copy = (Option)option.DeepCopy("Copy", "Copy");

        copy.DisallowedCharactersPattern.Should().Be(@"[<>]");
    }

    [RootConfigSection(sortOrder: 1)]
    [DisplayNameLocal("TestPage")]
    internal class PageWithRadioButtonsOption
    {
        [DisplayModeRadioButtons]
        [DisplayNameLocal("RadioOption")]
        public RadioOption RadioOption { get; set; }
    }

    [RootConfigSection(sortOrder: 1)]
    [DisplayNameLocal("TestPage")]
    internal class PageWithDisallowedCharactersOption
    {
        [DisallowedCharacters(@"[<>]")]
        [DisplayNameLocal("StringOption")]
        public string StringOption { get; set; } = string.Empty;
    }

    public enum RadioOption
    {
        [DisplayNameLocal("RadioOption_OptionA")]
        OptionA,

        [DisplayNameLocal("RadioOption_OptionB")]
        OptionB,
    }

}
