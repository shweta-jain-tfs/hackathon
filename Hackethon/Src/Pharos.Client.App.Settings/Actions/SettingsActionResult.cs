namespace Pharos.Client.App.Settings.Actions;

internal sealed record SettingsActionResult(IReadOnlyCollection<SettingsActionEffect> Effects)
{
    public static SettingsActionResult Empty { get; } = new([]);

    public static SettingsActionResult From(params SettingsActionEffect[] effects)
    {
        return new SettingsActionResult(effects);
    }
}

internal abstract record SettingsActionEffect;

internal sealed record SettingsActionToastEffect(SettingsActionToastType Type, string MessageResourceName) : SettingsActionEffect;

internal sealed record SettingsActionExportResultDialogEffect(string ErrorMessageResourceName) : SettingsActionEffect;

internal sealed record SettingsActionImportResultDialogEffect(
    bool IsErrorInvalidFile,
    IReadOnlyCollection<FailedSettingImportError> InvalidSettings,
    bool IsUnrecognizedSettings,
    IReadOnlyCollection<UnrecognizedSettingImportInfo> UnrecognizedSettings) : SettingsActionEffect;

internal sealed record SettingsActionNavigateToSettingsRootEffect : SettingsActionEffect;

internal enum SettingsActionToastType
{
    Information,
    Warning,
    Confirmation,
    Alarm
}