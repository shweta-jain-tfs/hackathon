using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Protocol-independent data export settings for a LIS connection.
/// </summary>
public class LisExportSettings
{
    /// <summary>
    /// Gets or sets the timeout for exports in minutes.
    /// </summary>
    [DisplayNameLocal("LisExportSettings_TimeoutForExports")]
    [SettingsRange(1, 1000)]
    public int TimeoutForExports { get; set; } = 60;

    [DisplayNameLocal("LisExportSettings_PeriodicExport")]
    public bool PeriodicExport { get; set; }

    /// <summary>
    /// Gets or sets the interval for periodic exports in minutes.
    /// </summary>
    [DisplayNameLocal("LisExportSettings_Interval")]
    [DependsFrom(nameof(PeriodicExport), "True", DependencyType.Disable)]
    [SettingsRange(1, 1000)]
    public int Interval { get; set; } = 5;

    /// <summary>
    /// Gets or sets data export settings.
    /// </summary>
    [SettingsGroup]
    [DisplayNameLocal("LisExportSettings_Sample")]
    public LisExportSampleSettings Sample { get; set; } = new();

    /// <summary>
    /// Gets or sets data export settings.
    /// </summary>
    [SettingsGroup]
    [DisplayNameLocal("LisExportSettings_QualityControl")]
    public LisExportQualityControlSettings QualityControl { get; set; } = new();

    /// <summary>
    /// Gets or sets data export settings.
    /// </summary>
    [SettingsGroup]
    [DisplayNameLocal("LisExportSettings_CurveControl")]
    public LisExportCurveControlSettings CurveControl { get; set; } = new();
}