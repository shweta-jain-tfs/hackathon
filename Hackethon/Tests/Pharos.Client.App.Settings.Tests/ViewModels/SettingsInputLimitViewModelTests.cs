using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.App.Settings.TestHelpers;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels;

public class SettingsInputLimitViewModelTests
{
    private readonly Mock<IStringLocalizer<Translations>> _localizer = LocalizerMockFactory.Create();
    private readonly Mock<ISettingsDataService<SettingItemBase>> _dataService = new();

    [Theory]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(int.MaxValue, null)]
    [InlineData(12, 12)]
    [InlineData(2147483646, 2147483646)]
    public void StringViewModel_InputMaxLength_UsesConfiguredMaxLength(int maxLength, int? expected)
    {
        var viewModel = new StringViewModel(_localizer.Object, _dataService.Object)
        {
            MaxLength = maxLength
        };

        viewModel.InputMaxLength.Should().Be(expected);
    }

    [Fact]
    public void PasswordViewModel_InputMaxLength_UsesConfiguredMaxLength()
    {
        var viewModel = new PasswordViewModel(_localizer.Object, _dataService.Object)
        {
            MaxLength = 12
        };

        viewModel.InputMaxLength.Should().Be(12);
    }
}
