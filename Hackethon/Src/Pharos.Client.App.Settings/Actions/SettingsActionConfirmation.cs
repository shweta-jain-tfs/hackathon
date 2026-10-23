namespace Pharos.Client.App.Settings.Actions;

internal sealed record SettingsActionConfirmation(SettingsActionConfirmationType Type);

internal enum SettingsActionConfirmationType
{
    DeleteExistingSettings
}