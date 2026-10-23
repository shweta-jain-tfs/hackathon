using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Protocol-independent data export settings for a LIS connection.
/// </summary>
public class LisExportCurveControlSettings
{
    [DisplayNameLocal("LisExportCurveControlSettings_ExportCurveControls")]
    public bool ExportCurveControls { get; set; }

    [DisplayNameLocal("LisExportCurveControlSettings_AutomaticallyWhenCCMeasured")]
    [DependsFrom(nameof(ExportCurveControls), "True", DependencyType.Disable)]
    public bool AutomaticallyWhenCcMeasured { get; set; }

    [DisplayNameLocal("LisExportCurveControlSettings_AutomaticallyWhenRunApprovedOrRejected")]
    [DependsFrom(nameof(ExportCurveControls), "True", DependencyType.Disable)]
    public bool AutomaticallyWhenRunApprovedOrRejected { get; set; }

    [DisplayNameLocal("LisExportCurveControlSettings_CheckWhenManuallyExportingSamples")]
    [DependsFrom(nameof(ExportCurveControls), "True", DependencyType.Disable)]
    public bool CheckWhenManuallyExportingSamples { get; set; }
}
