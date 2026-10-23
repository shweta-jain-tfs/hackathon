using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Database;

public class AutomaticDeleteSettings
{
    [DisplayNameLocal("DatabaseSettings_AutomaticDelete_DeleteRequestsAutomatically")]
    public bool DeleteRequestsAutomatically { get; set; }

    [SettingsRange(1, 1000)]
    [DisplayNameLocal("DatabaseSettings_AutomaticDelete_DeleteRequestsOlderThan")]
    [DependsFrom(nameof(DeleteRequestsAutomatically), "True", DependencyType.Disable)]
    public int DeleteRequestsOlderThanDays { get; set; } = 365;

    [DisplayNameLocal("DatabaseSettings_AutomaticDelete_DeleteResultsAutomatically")]
    public bool DeleteResultsAutomatically { get; set; }

    [SettingsRange(1, 1000)]
    [DisplayNameLocal("DatabaseSettings_AutomaticDelete_DeleteResultsOlderThan")]
    [DependsFrom(nameof(DeleteResultsAutomatically), "True", DependencyType.Disable)]
    public int DeleteResultsOlderThanDays { get; set; } = 365;

    [DisplayNameLocal("DatabaseSettings_AutomaticDelete_DeleteQualityAutomatically")]
    public bool DeleteQualityAutomatically { get; set; }

    [SettingsRange(28, 1000)]
    [DisplayNameLocal("DatabaseSettings_AutomaticDelete_DeleteQualityOlderThan")]
    [DependsFrom(nameof(DeleteQualityAutomatically), "True", DependencyType.Disable)]
    public int DeleteQualityOlderThanDays { get; set; } = 365;
}