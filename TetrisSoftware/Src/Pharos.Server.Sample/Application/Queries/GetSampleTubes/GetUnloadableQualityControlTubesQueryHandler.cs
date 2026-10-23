using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Sample;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTubes;

internal class GetUnloadableQualityControlTubesQueryHandler(
    ICurrentInstrumentService currentInstrument,
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext,
    IInstrumentManager instrumentManager,
    ISender sender) : IRequestHandler<GetUnloadableQualityControlTubesQuery, List<UnloadableQualityControlTubeDto>>
{
    public async Task<List<UnloadableQualityControlTubeDto>> Handle(GetUnloadableQualityControlTubesQuery request, CancellationToken cancellationToken)
    {
        var tubes = sampleStorage.GetTubesBySampleId(request.QualityControlName, includeTubesWithError: true)
            .Where(tube => tube.SampleType == SampleType.QualityControl
                && !string.IsNullOrEmpty(tube.TubeId)
                && !string.IsNullOrEmpty(tube.InstrumentNumber))
            .OrderByDescending(tube => tube.LoadTimestamp);

        var result = new List<UnloadableQualityControlTubeDto>();
        foreach (var tube in tubes)
        {
            var dto = tube.ToUnloadableQualityControlTubeDto();
            dto.IsInCurrentInstrument = tube.InstrumentNumber == currentInstrument.InstrumentNumber;
            dto.InstrumentName = instrumentManager.FindInstrument(tube.InstrumentNumber)?.DisplayName;
            if (!string.IsNullOrEmpty(dto.TrayId))
            {
                dto.DoesTrayExist = sampleStorage.IsTrayInAnyInstrument(dto.TrayId)
                    || await dbContext.SampleTrays.AnyAsync(tray => tray.Id == dto.TrayId, cancellationToken);
            }

            if (!string.IsNullOrEmpty(tube.TubeBarcode))
            {
                var qc = await sender.Send(new IsQualityControlQuery(tube.TubeBarcode, tube.InstrumentNumber!), cancellationToken);
                if (qc.IsQualityControl && qc.QualityControlIdentity == request.QualityControlName)
                {
                    dto.Lot = qc.LotNumber;
                }
            }
            
            result.Add(dto);
        }

        return result;
    }
}
