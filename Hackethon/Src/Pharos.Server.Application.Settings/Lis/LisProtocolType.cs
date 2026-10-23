using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

/// <summary>
/// Specifies the high-level LIS communication protocols (e.g. ASTM or DataCAP) supported by Pharos.
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum LisProtocolType
{
    /// <summary>
    /// The ASTM protocol
    /// </summary>
    [DisplayNameLocal("LisProtocolType_Astm")]
    Astm = 1,

    /// <summary>
    /// The HL7 protocol.
    /// </summary>
    [DisplayNameLocal("LisProtocolType_HL7")]
    HL7 = 2,

    /// <summary>
    /// The DataCAP protocol
    /// </summary>
    [DisplayNameLocal("LisProtocolType_DataCap")]
    DataCap = 3
}
