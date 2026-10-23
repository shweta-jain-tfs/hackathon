using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public class DataCapSettings
{
    [DisplayNameLocal("DataCapSettings_LisDataCapMessageSettings")]
    public DataCapMessageSettings MessageSettings { get; set; }

    [DisplayNameLocal("DataCapSettings_LisDataCapFtpSettings")]
    public DataCapFtpSettings FtpSettings { get; set; }

    [DisplayNameLocal("DataCapSettings_LisDataCapOffLineSettings")]
    public DataCapOffLineSettings OffLineSettings { get; set; }

    [DisplayNameLocal("DataCapSettings_UseFileHeaderSection")]
    public bool UseFileHeaderSection { get; set; }

    [DisplayNameLocal("DataCapSettings_OnlyResultsForApprovedTests")]
    public bool OnlyResultsForApprovedTests { get; set; }

    [DisplayNameLocal("DataCapSettings_ImportedDirectory")]
    public string ImportedDirectory { get; set; } //TODO: hardcoded?

    [DisplayNameLocal("DataCapSettings_ExportedDirectory")]
    public string ExportedDirectory { get; set; } //TODO: hardcoded?

    public DataCapSettings()
    {
        MessageSettings = new DataCapMessageSettings();
        FtpSettings = new DataCapFtpSettings();
        OffLineSettings = new DataCapOffLineSettings();
        UseFileHeaderSection = true;
        OnlyResultsForApprovedTests = false;
        ImportedDirectory = @"%PROGRAMDATA%\Thermo Fisher Scientific\Pharos\LIS\DataCap\Imported\";
        ExportedDirectory = @"%PROGRAMDATA%\Thermo Fisher Scientific\Pharos\LIS\DataCap\Exported\";
    }
}