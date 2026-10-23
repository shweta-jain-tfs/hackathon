namespace Pharos.Server.Application.Settings;

public enum ActionButtonType
{
    Primary,
    Secondary,
    Text
}

public enum ActionKey
{
    Import,
    Export
}

/// <summary>
/// Represents a settings UI action instead of a persisted configuration value.
/// </summary>
public sealed class SettingsAction(
    ActionKey actionKey,
    ActionButtonType buttonType = ActionButtonType.Secondary)
{
    public ActionKey ActionKey { get; } = actionKey;
    public ActionButtonType ButtonType { get; } = buttonType;
}
