using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Configuration;

[RootConfigSection(sortOrder: 1100)]
[DisplayNameLocal("ConfigurationSettings")]
public class ConfigurationSettings
{
    [SettingsGroup]
    [DisplayNameLocal("ConfigurationSettings_ImportAndExportSettings")]
    public ImportExportSettings ImportExport { get; set; } = new();
}

public class ImportExportSettings
{
    [DisplayNameLocal("ConfigurationSettings_Import")]
    [HelpText(typeof(string), "ConfigurationSettings_ImportSettingsFromFile")]
    public SettingsAction ImportButton { get; set; } = new(ActionKey.Import, ActionButtonType.Secondary);

    [DisplayNameLocal("ConfigurationSettings_Export")]
    [HelpText(typeof(string), "ConfigurationSettings_ExportSettingsToFile")]
    public SettingsAction ExportButton { get; set; } = new(ActionKey.Export, ActionButtonType.Secondary);
}
