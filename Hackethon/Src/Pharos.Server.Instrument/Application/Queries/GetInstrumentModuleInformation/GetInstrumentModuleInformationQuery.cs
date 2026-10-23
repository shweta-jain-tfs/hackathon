using MediatR;
using Pharos.Server.Application.Instrument;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentModuleInformation;

internal record GetInstrumentModuleInformationQuery(int Id) : IRequest<ModuleInformationDto>;

internal class GetInstrumentModuleInformationQueryHandler(
    IInstrumentManager stateManager) : IRequestHandler<GetInstrumentModuleInformationQuery, ModuleInformationDto>
{
    public Task<ModuleInformationDto> Handle(GetInstrumentModuleInformationQuery request, CancellationToken cancellationToken)
    {
        var instrument = (Domain.Instrument)stateManager.GetInstrument(request.Id);

        var dto = new ModuleInformationDto
        {
            SerialNumber = instrument.SerialNumber,
            InstallationDate = instrument.InstallationDate ?? DateTimeOffset.MinValue,
            Type = instrument.InstrumentType,
            Modules = instrument.Modules?.Select(m => new ModuleDto
            {
                ModuleType = m.ModuleType,
                ArticleNumber = m.ArticleNumber,
                Model = m.Model,
                Version = m.Version,
                SerialNumber = m.SerialNumber,
                InstallationDate = m.InstallationDate,
            }).ToList() ?? []
        };

        return Task.FromResult(dto);
    }
}