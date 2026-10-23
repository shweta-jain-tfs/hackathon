using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class AstmImportSettings
{
    [DisplayNameLocal("AstmImportSettings_UseLaboratoryPidAsPatientId")]
    public bool UseLaboratoryPidAsPatientId { get; set; }

    [DisplayNameLocal("AstmImportSettings_SkipImportOfRequestorInformation")]
    public bool SkipImportOfRequestorInformation { get; set; }

    [DisplayNameLocal("AstmImportSettings_IgnoreImportedPreDilutionFactor")]
    public bool IgnoreImportedPreDilutionFactor { get; set; } = true;

    [DisplayNameLocal("AstmImportSettings_IgnoreImportedInstrumentDilutionFactor")]
    public bool IgnoreImportedInstrumentDilutionFactor { get; set; } = true;

    [DisplayNameLocal("AstmImportSettings_IgnoreImportedNumberOfReplicates")]
    public bool IgnoreImportedNumberOfReplicates { get; set; }

    [DisplayNameLocal("AstmImportSettings_IgnoreNonCriticalImportErrors")]
    public bool IgnoreNonCriticalImportErrors { get; set; }
}