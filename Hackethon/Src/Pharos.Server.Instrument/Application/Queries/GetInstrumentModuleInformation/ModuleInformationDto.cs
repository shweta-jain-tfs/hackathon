using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentModuleInformation;
public class ModuleInformationDto
{
    public int Id { get; set; }
    public string? SerialNumber { get; set; }

    public DateTimeOffset InstallationDate { get; set; }

    public InstrumentType Type { get; set; }

    public List<ModuleDto>? Modules { get; set; }
}