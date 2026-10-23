namespace Pharos.Client.App.Settings.Actions;

internal enum SettingsActionScriptSource
{
    SettingsModule,
    CommonWeb
}

internal readonly record struct SettingsActionScript(string FileName, SettingsActionScriptSource Source);