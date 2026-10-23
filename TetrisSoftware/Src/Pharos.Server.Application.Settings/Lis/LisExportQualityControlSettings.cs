using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Protocol-independent data export settings for a LIS connection.
/// </summary>
public class LisExportQualityControlSettings
{
    [DisplayNameLocal("LisExportQualityControlSettings_ExportQualityControls")]
    public bool ExportQualityControls { get; set; }

    [DisplayNameLocal("LisExportQualityControlSettings_AutomaticallyWhenQCMeasured")]
    [DependsFrom(nameof(ExportQualityControls), "True", DependencyType.Disable)]
    public bool AutomaticallyWhenQcMeasured { get; set; }

    [DisplayNameLocal("LisExportQualityControlSettings_AutomaticallyWhenRunApprovedOrRejected")]
    [DependsFrom(nameof(ExportQualityControls), "True", DependencyType.Disable)]
    public bool AutomaticallyWhenRunApprovedOrRejected { get; set; }

    [DisplayNameLocal("LisExportQualityControlSettings_CheckWhenManuallyExportingSamples")]
    [DependsFrom(nameof(ExportQualityControls), "True", DependencyType.Disable)]
    public bool CheckWhenManuallyExportingSamples { get; set; }
}
