using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels.Converters;

public class ActionButtonConverterTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = new();
    private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService = new();
    private readonly Mock<IConverter> _mockConverter = new();

    [Fact]
    public void Type_ShouldReturnActionButtonType()
    {
        var converter = new ActionButtonConverter(_mockLocalizer.Object);

        converter.Type.Should().Be<ActionButton>();
    }

    [Fact]
    public void TryConvert_ValidItem_ReturnsTrueAndCreatesViewModel()
    {
        // Arrange
        var converter = new ActionButtonConverter(_mockLocalizer.Object);
        var actionButton = new ActionButton
        {
            ActionKey = ActionKey.Export,
            LabelHelpText = "Export settings to file"
        };

        // Act
        var success = converter.TryConvert(actionButton, _mockConverter.Object, _mockDataService.Object, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().BeOfType<ActionButtonViewModel>();
        result.As<ActionButtonViewModel>().ActionKey.Should().Be(ActionKey.Export);
        result.As<ActionButtonViewModel>().Model.Should().Be(actionButton);
        result.As<ActionButtonViewModel>().LabelHelpText.Should().Be("Export settings to file");
    }

    [Fact]
    public void TryConvert_InvalidItem_ReturnsFalse()
    {
        // Arrange
        var converter = new ActionButtonConverter(_mockLocalizer.Object);
        var option = new Option();

        // Act
        var success = converter.TryConvert(option, _mockConverter.Object, _mockDataService.Object, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryConvert_ValidItem_ImportActionKey_IsPreserved()
    {
        // Arrange
        var converter = new ActionButtonConverter(_mockLocalizer.Object);
        var actionButton = new ActionButton
        {
            ActionKey = ActionKey.Import,
            LabelHelpText = "Import settings from file"
        };

        // Act
        var success = converter.TryConvert(actionButton, _mockConverter.Object, _mockDataService.Object, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().BeOfType<ActionButtonViewModel>();
        result.As<ActionButtonViewModel>().ActionKey.Should().Be(ActionKey.Import);
        result.As<ActionButtonViewModel>().Model.Should().Be(actionButton);
        result.As<ActionButtonViewModel>().LabelHelpText.Should().Be("Import settings from file");
    }
}