using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class LisOtherSettings
{

    /// <summary>
    /// Gets or sets Reset Statistics At Midnight
    /// </summary>
    [DisplayNameLocal("LisSettings_LisChannel_ResetStatisticsAtMidnight")]
    public bool ResetStatisticsAtMidnight { get; set; } = true;
}
