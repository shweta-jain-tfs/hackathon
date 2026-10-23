using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTrayList;

internal record GetSampleTrayQuery(string Id) : IRequest<SampleTrayDto>;

internal sealed class GetSampleTrayQueryHandler(ISampleDbContext dbContext, IInstrumentManager instrumentManager) : IRequestHandler<GetSampleTrayQuery, SampleTrayDto>
{
    public async Task<SampleTrayDto> Handle(GetSampleTrayQuery request, CancellationToken cancellationToken)
    {
        var sampleTray = await dbContext.SampleTrays.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (sampleTray == null)
        {
            throw new NotFoundException(nameof(SampleTray), request.Id);
        }

        string? instrumentName = null;
        if (!string.IsNullOrEmpty(sampleTray.InstrumentNumber))
        {
            instrumentName = instrumentManager.FindInstrument(sampleTray.InstrumentNumber)?.DisplayName ?? sampleTray.InstrumentNumber;
        }

        return new SampleTrayDto
        {
            Id = sampleTray.Id,
            InstrumentName = instrumentName ?? Translations.TrayLocation_NotLoaded
        };
    }
}
