using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public enum WhatToExport
{
    [DisplayNameLocal("LisExportSampleSettings_WhatToExport_AllStatuses")]
    AllStatuses = 0,

    [DisplayNameLocal("LisExportSampleSettings_WhatToExport_OnlyApprovedResults")]
    OnlyApprovedResults = 1,

    [DisplayNameLocal("LisExportSampleSettings_WhatToExport_OnlyApprovedAndRejectedResults")]
    OnlyApprovedAndRejectedResults = 2
}
