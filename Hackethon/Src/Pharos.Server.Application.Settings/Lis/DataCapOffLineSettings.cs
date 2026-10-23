using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Lis;

public record DataCapOffLineSettings
{
    [DisplayNameLocal("DataCapOffLineSettings_ConnectionMode")]
    public LisConnectionMode ConnectionMode { get; set; } = LisConnectionMode.Server;

    [DisplayNameLocal("DataCapOffLineSettings_RootFolder")]
    public string RootFolder { get; set; }

    [DisplayNameLocal("DataCapOffLineSettings_ExportDirectory")]
    public string ExportDirectory { get; set; }

    [DisplayNameLocal("DataCapOffLineSettings_ImportDirectory")]
    public string ImportDirectory { get; set; }

    [DisplayNameLocal("DataCapOffLineSettings_UseStandardFileNaming")]
    public bool UseStandardFileNaming { get; set; }
    public DataCapOffLineSettings()
    {
        ExportDirectory = @"%PROGRAMDATA%\Thermo Fisher Scientific\Pharos\LIS\DataCap\Export\";
        ImportDirectory = @"%PROGRAMDATA%\Thermo Fisher Scientific\Pharos\LIS\DataCap\Import\";
        RootFolder = @"%PROGRAMDATA%\Thermo Fisher Scientific\Pharos\LIS\DataCap\";
    }
}