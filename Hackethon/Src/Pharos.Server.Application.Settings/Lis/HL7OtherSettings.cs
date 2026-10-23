using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class HL7OtherSettings
{
    [DisplayNameLocal("HL7OtherSettings_NonUniqueOrderingPhysicianCode")]
    public bool NonUniqueOrderingPhysicianCode { get; set; }

    [DisplayNameLocal("HL7OtherSettings_AllowExtraLongPatientComments")]
    public bool AllowExtraLongPatientComments { get; set; }
}
