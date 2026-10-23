using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentModuleInformation;

public class ModuleDto
{
    public ModuleType ModuleType { get; set; }
    public string? ArticleNumber { get; set; }
    public string? Model { get; set; }
    public string? Version { get; set; }
    public string? SerialNumber { get; set; }
    public DateTimeOffset? InstallationDate { get; set; }
}
