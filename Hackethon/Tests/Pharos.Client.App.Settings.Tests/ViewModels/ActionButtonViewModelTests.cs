using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Settings.ViewModels;

public class ActionButtonViewModelTests
{
    private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer = new();

    [Fact]
    public void ActionKey_WhenModelIsActionButton_ReturnsActionKey()
    {
        var viewModel = new ActionButtonViewModel(_mockLocalizer.Object)
        {
            Model = new ActionButton
            {
                ActionKey = ActionKey.Import
            }
        };

        viewModel.ActionKey.Should().Be(ActionKey.Import);
    }

    [Fact]
    public void ActionKey_WhenModelIsMissing_ThrowsInvalidOperationException()
    {
        var viewModel = new ActionButtonViewModel(_mockLocalizer.Object);

        var action = () => _ = viewModel.ActionKey;

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("ActionButton model is missing.");
    }

    [Fact]
    public void ButtonType_WhenModelIsMissing_ReturnsSecondary()
    {
        var viewModel = new ActionButtonViewModel(_mockLocalizer.Object);

        viewModel.ButtonType.Should().Be(ActionButtonType.Secondary);
    }
}