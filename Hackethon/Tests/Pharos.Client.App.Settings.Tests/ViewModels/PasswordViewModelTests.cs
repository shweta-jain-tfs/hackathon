using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels;

public class PasswordViewModelTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = LocalizerMockFactory.Create();
    private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService = new();

    [Fact]
    public void ClearOnFirstInteraction_ShouldClearValue()
    {
        var viewModel = new PasswordViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option { Key = "test:Password" },
            Value = "secret123",
        };

        viewModel.ClearOnFirstInteraction();

        viewModel.Value.Should().BeEmpty();
        viewModel.IsCleared.Should().BeTrue();
    }

    [Fact]
    public void ClearOnFirstInteraction_ShouldOnlyClearOnce()
    {
        var viewModel = new PasswordViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = new Option { Key = "test:Password" },
            Value = "secret123",
        };

        viewModel.ClearOnFirstInteraction();
        viewModel.Value = "newPassword";
        viewModel.ClearOnFirstInteraction();

        viewModel.Value.Should().Be("newPassword");
    }

    [Fact]
    public void PasswordViewModel_ShouldInheritStringViewModelValidation()
    {
        var option = new Option
        {
            Key = "test:Password",
            Required = true,
        };
        var viewModel = new PasswordViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            Value = "",
        };
        viewModel.Validate();

        viewModel.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void PasswordViewModel_ShouldRespectMaxLength()
    {
        var option = new Option
        {
            Key = "test:Password",
        };
        var viewModel = new PasswordViewModel(_mockLocalizer.Object, _mockDataService.Object)
        {
            Model = option,
            MaxLength = 5,
            Value = "123456",
        };
        viewModel.Validate();

        viewModel.ValidationErrors.Should().NotBeEmpty();
    }
}
