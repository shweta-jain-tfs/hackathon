using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Security;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Application.Mapping;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTubeHistory;

internal class GetSampleTubeHistoryQueryHandler(
    ICurrentInstrumentService currentInstrument,
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext,
    IInstrumentManager instrumentManager,
    IAuthorizationService authorizationService) : IRequestHandler<GetSampleTubeHistoryQuery, List<SampleTubeHistoryDto>>
{
    public async Task<List<SampleTubeHistoryDto>> Handle(GetSampleTubeHistoryQuery request, CancellationToken cancellationToken)
    {
        // 1. Get tubes from sample storage (in-memory)
        var storageTubes = sampleStorage.GetTubesBySampleId(request.SampleId, includeTubesWithError: true);

        // 2. Get tubes from DB for the same SampleId and RequestCreatedDate
        // Exclude tubes that do not have a LoadTimestamp. Predefined tubes(manually entry) will not have a LoadTimestamp until they are detected by the instrument.
        var dbTubes = await dbContext.SampleTubes
            .Where(t =>
                t.SampleId == request.SampleId &&
                t.RequestCreatedDate == request.RequestCreatedDate &&
                t.LoadTimestamp != null)
            .ToListAsync(cancellationToken);

        // 3. Exclude tubes from DB that are present in storage (match by SampleId and LoadTimestamp)
        var dbTubesFiltered = dbTubes
            .Where(t => storageTubes.Find(x => x.InstrumentNumber == t.InstrumentNumber && x.SampleId == t.SampleId && x.LoadTimestamp == t.LoadTimestamp) == null)
            .ToList();

        // 4. Map to DTOs and set IsInCurrentInstrument correctly
        var currentInstrumentNumber = currentInstrument.InstrumentNumber;

        var result = storageTubes
            .Select(tube =>
            {
                var dto = tube.ToSampleTubeHistoryDto();
                dto.IsInCurrentInstrument = tube.InstrumentNumber == currentInstrumentNumber;
                return dto;
            })
            .ToList();

        // 5. Combine with DB tubes
        result.AddRange(dbTubesFiltered.ToSampleTubeHistoryDtos());

        // 6. Set InstrumentName for instruments
        result.ForEach(x => x.InstrumentName = instrumentManager.FindInstrument(x.InstrumentNumber)?.DisplayName);

        // 7. Find out if the tray exists
        foreach (var sampleTubeHistoryDto in result.Where(sampleTubeHistoryDto => !string.IsNullOrEmpty(sampleTubeHistoryDto.TrayId)))
        {
            sampleTubeHistoryDto.DoesTrayExist = sampleStorage.IsTrayInAnyInstrument(sampleTubeHistoryDto.TrayId!) || await dbContext.SampleTrays.AnyAsync(t => t.Id == sampleTubeHistoryDto.TrayId, cancellationToken);
        }

        // 8. Return, ordered by LoadTimestamp descending
        result = result
            .OrderByDescending(x => x.LoadTimestamp)
            .ToList();

        // 9. Hide image identity if user has no permission
        var allowedToSeeTubeImages = authorizationService.RndMode || await authorizationService.IsCurrentUserInRoleAsync(BuiltInRoles.ServiceEngineer);
        if (!allowedToSeeTubeImages)
        {
            result.ForEach(x => x.ImageIdentity = null);
        }

        return result;
    }
}