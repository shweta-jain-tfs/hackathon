using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Database;

[RootConfigSection(sortOrder: 100)]
[DisplayNameLocal("DatabaseSettings")]
public class DatabaseSettings
{
    [SettingsGroup]
    [DisplayNameLocal("DatabaseSettings_AutomaticDelete")]
    public AutomaticDeleteSettings AutomaticDelete { get; set; } = new AutomaticDeleteSettings();
}
