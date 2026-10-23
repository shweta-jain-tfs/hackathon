using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public class ActionButtonViewModel(IStringLocalizer<Translations> loc) : SettingsViewModelBase(loc)
{
    private ActionButton? ModelAsActionButton => Model as ActionButton;

    public ActionKey ActionKey => ModelAsActionButton?.ActionKey
        ?? throw new InvalidOperationException("ActionButton model is missing.");

    public ActionButtonType ButtonType => ModelAsActionButton?.ButtonType ?? ActionButtonType.Secondary;
}