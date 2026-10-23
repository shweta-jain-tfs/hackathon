using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Protocol-independent data export settings for a LIS connection.
/// </summary>
public class LisExportSampleSettings
{
    [DisplayNameLocal("LisExportSampleSettings_AutomaticSampleExport")]
    public LisAutoExportTrigger AutomaticSampleExport { get; set; } = LisAutoExportTrigger.NotUsed;

    [DisplayNameLocal("LisExportSampleSettings_IncludeLocallyEnteredSamples")]
    public bool IncludeLocallyEnteredSamples { get; set; }

    [DisplayNameLocal("LisExportSampleSettings_DeleteSamplesAfterExport")]
    public bool DeleteSamplesAfterExport { get; set; }

    [DisplayNameLocal("LisExportSampleSettings_WhatToExport")]
    public WhatToExport WhatToExport { get; set; } = WhatToExport.AllStatuses;

    [DisplayNameLocal("LisExportSampleSettings_DuringRetestIncludeRejectedResults")]
    [DependsFrom(new[] { nameof(WhatToExport), nameof(WhatToExport) }, new[] { nameof(WhatToExport.AllStatuses), nameof(WhatToExport.OnlyApprovedAndRejectedResults) }, DependencyType.Disable)]
    public bool DuringRetestIncludeRejectedResults { get; set; }

    [DisplayNameLocal("LisExportSampleSettings_DuringRetestIncludeNewResults")]
    [DependsFrom(nameof(WhatToExport), nameof(WhatToExport.AllStatuses), DependencyType.Disable)]
    public bool DuringRetestIncludeNewResults { get; set; }

    [DisplayNameLocal("LisExportSampleSettings_ExportTestsInAlphabeticalOrder")]
    public bool ExportTestsInAlphabeticalOrder { get; set; }

    [DisplayNameLocal("LisExportSampleSettings_IncludeOtherTestsInAutomaticExports")]
    [DependsFrom(new[] { nameof(AutomaticSampleExport), nameof(AutomaticSampleExport) }, new[] { nameof(LisAutoExportTrigger.WhenTestIsApprovedOrRejected), nameof(LisAutoExportTrigger.WhenSampleIsApprovedOrRejected) }, DependencyType.Disable)]
    public IncludeOtherTestsInAutomaticExports IncludeOtherTestsInAutomaticExports { get; set; } = IncludeOtherTestsInAutomaticExports.NotUsed;
}