using Pharos.Server.Application.Settings.Result;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class ResultSettingsTests
{
    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public void ResultExportSettings_AnalyticalRunExportPath_ShouldDependOn_ExportOnAnalyticalRunEnd()
    {
        var assembly = typeof(ResultSettings).Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly });

        var pages = SettingsSearchPagesProvider.GetTopLevelPages(localizer: null!);

        var resultPage = pages.Values.Single(p => p.Key == "Result");
        var exportGroup = resultPage.Children.OfType<Group>()
            .Single(g => g.Children.OfType<Option>()
                .Any(o => o.Key.Contains(nameof(ResultExportSettings.AnalyticalRunExportPath))));

        var analyticalPath = exportGroup.Children.OfType<Option>()
            .Single(o => o.Key.Contains(nameof(ResultExportSettings.AnalyticalRunExportPath)));
        var customPath = exportGroup.Children.OfType<Option>()
            .Single(o => o.Key.Contains(nameof(ResultExportSettings.CustomRunExportPath)));

        analyticalPath.Dependency.Should().ContainSingle(d =>
            d.MainOptionKey.Contains(nameof(ResultExportSettings.ExportOnAnalyticalRunEnd)) &&
            d.MainOptionValue == "true");

        customPath.Dependency.Should().ContainSingle(d =>
            d.MainOptionKey.Contains(nameof(ResultExportSettings.ExportOnCustomRunEnd)) &&
            d.MainOptionValue == "true");
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public void ResultExportSettings_CustomRunFields_ShouldBeShowOnlyInRndMode()
    {
        var assembly = typeof(ResultSettings).Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly });

        var pages = SettingsSearchPagesProvider.GetTopLevelPages(localizer: null!);

        var resultPage = pages.Values.Single(p => p.Key == "Result");
        var exportGroup = resultPage.Children.OfType<Group>()
            .Single(g => g.Children.OfType<Option>()
                .Any(o => o.Key.Contains(nameof(ResultExportSettings.ExportOnCustomRunEnd))));

        var customRunCheckbox = exportGroup.Children.OfType<Option>()
            .Single(o => o.Key.Contains(nameof(ResultExportSettings.ExportOnCustomRunEnd)));
        var customRunPath = exportGroup.Children.OfType<Option>()
            .Single(o => o.Key.Contains(nameof(ResultExportSettings.CustomRunExportPath)));

        customRunCheckbox.ShowOnlyInRndMode.Should().BeTrue();
        customRunPath.ShowOnlyInRndMode.Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public void ResultExportSettings_PathProperties_ShouldBeRequired()
    {
        // [Required] is what drives "Value for Path is required" in the UI — verify it is present.
        typeof(ResultExportSettings)
            .GetProperty(nameof(ResultExportSettings.AnalyticalRunExportPath))!
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false)
            .Should().ContainSingle();

        typeof(ResultExportSettings)
            .GetProperty(nameof(ResultExportSettings.CustomRunExportPath))!
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false)
            .Should().ContainSingle();
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Fact]
    public void ResultExportSettings_PathProperties_ShouldHaveAbsoluteDirectoryPathAttribute()
    {
        typeof(ResultExportSettings)
            .GetProperty(nameof(ResultExportSettings.AnalyticalRunExportPath))!
            .GetCustomAttributes(typeof(Pharos.Server.Application.Settings.Attributes.AbsoluteDirectoryPathAttribute), false)
            .Should().ContainSingle();

        typeof(ResultExportSettings)
            .GetProperty(nameof(ResultExportSettings.CustomRunExportPath))!
            .GetCustomAttributes(typeof(Pharos.Server.Application.Settings.Attributes.AbsoluteDirectoryPathAttribute), false)
            .Should().ContainSingle();
    }

    [Trait("ProductRequirement", "PR_2136")]
    [Trait("ModuleRequirement", "MR_5805")]
    [Theory]
    [InlineData("C:\\Export\\Results", true)]
    [InlineData("C:/Export/Results", true)]
    [InlineData("XX", false)]
    [InlineData("relative\\path", false)]
    [InlineData("../relative", false)]
    // Empty is intentionally valid here: the class default is "" and ValueValidator runs
    // at startup against default values. The "not empty" constraint is enforced separately
    // at save time via [Required] → Option.Required = true → "Value for Path is required".
    [InlineData("", true)]
    public void IsValidAbsoluteDirectoryPath_ShouldValidateCorrectly(string path, bool expected)
    {
        ValueValidator.IsValidAbsoluteDirectoryPath(path).Should().Be(expected);
    }
}