using Pharos.Server.Application.Settings;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Application.Metadata;

public class ActionButton : SettingsChildItem
{
    public ActionKey ActionKey { get; set; }
    public ActionButtonType ButtonType { get; set; } = ActionButtonType.Secondary;

    public override SettingsChildItem DeepCopy(string settingsKeyPrefix, string parentNavigationPath)
    {
        var dependency = string.IsNullOrEmpty(settingsKeyPrefix) ? Dependency : PageHelper.CopyDependency(Dependency, settingsKeyPrefix);

        return new ActionButton
        {
            Key = settingsKeyPrefix + PageHelper.SettingsKeyDeLimiter + PageHelper.GetSuffixKeyName(Key),
            Label = Label,
            LabelHelpText = LabelHelpText,
            Url = Url,
            Dependency = dependency,
            Permissions = Permissions,
            ActionKey = ActionKey,
            ButtonType = ButtonType,
            ShowOnlyInRndMode = ShowOnlyInRndMode
        };
    }
}