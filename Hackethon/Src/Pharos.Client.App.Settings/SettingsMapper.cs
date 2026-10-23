using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.Settings;

[Mapper]
public static partial class SettingsMapper
{
    public static UpdateSettingCommand ToUpdateSettingCommand(this Page source)
    {
        var command = ToUpdateSettingCommandCore(source);
        command.SettingToSave = source;
        return command;
    }

    [MapProperty(nameof(Page.Label), nameof(UpdateSettingCommand.Value))]
    [MapperIgnoreTarget(nameof(UpdateSettingCommand.SettingToSave))]
    [MapperIgnoreTarget(nameof(UpdateSettingCommand.IsPageListUpdate))]
    private static partial UpdateSettingCommand ToUpdateSettingCommandCore(Page source);
}
