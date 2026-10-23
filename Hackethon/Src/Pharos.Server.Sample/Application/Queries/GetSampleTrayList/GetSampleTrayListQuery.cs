using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Localization;
using Pharos.Server.Sample.Application.Interfaces;
using SampleTray = Pharos.Server.Sample.Domain.SampleTray;

namespace Pharos.Server.Sample.Application.Queries.GetSampleTrayList;

public class GetSampleTrayListQuery : SortedPartialQuery<SampleTrayDto>
{
    public string? TrayId { get; set; }
    public List<string>? TrayLocation { get; set; }
    public bool HasTubeIssues { get; set; }
    public bool BypassRowLimit { get; set; }
}

internal class GetSampleTrayListQueryHandler(
    IInstrumentSampleStorage sampleStorage,
    ISampleDbContext dbContext,
    IInstrumentManager instrumentManager) : IRequestHandler<GetSampleTrayListQuery, PartialQueryResult<SampleTrayDto>>
{
    public async Task<PartialQueryResult<SampleTrayDto>> Handle(GetSampleTrayListQuery request, CancellationToken cancellationToken)
    {
        var instrumentTrays = sampleStorage.GetTrays()
            .Where(x => !string.IsNullOrEmpty(x.Id) && x.Error == Domain.SampleTrayError.None
                && instrumentManager.FindInstrument(x.InstrumentNumber!)?.DrawersLockState[x.Drawer!.Value].LockState != LockState.Open)
            .ToDictionary(x => x.Id);

        var allTrays = new List<SampleTray>(instrumentTrays.Values);

        var unloadedTrays = await dbContext.SampleTrays
            .Where(x => !instrumentTrays.Keys.Contains(x.Id))
            .Select(x => new SampleTray
            {
                Id = x.Id,
                FreePositions = Constants.MaxSampleTubesInTray - x.SampleTubes.Count(),
                TubeErrorCount = x.SampleTubes.Count(t => t.InstrumentError != SampleTubeError.None || t.InternalError != SampleTubeError.None),
                InstrumentNumber = x.InstrumentNumber
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        allTrays.AddRange(unloadedTrays);

        var filteredTrays = FilterByTrayIdAndLocation(allTrays, request);
        filteredTrays = FilterByError(filteredTrays, request);
        filteredTrays = filteredTrays.ToList();

        var projectedTrays = filteredTrays.Select(x => new SampleTrayDto
        {
            Id = x.Id,
            InstrumentName = GetInstrumentName(x.InstrumentNumber),
            ErrorCount = x.TubeErrorCount,
            FreePositions = x.FreePositions
        });

        var sortedTrays = SortTrays(projectedTrays, request.SortByProperty, request.SortByAscending);

        var pagedTrays = sortedTrays
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        return new PartialQueryResult<SampleTrayDto>(pagedTrays, request.StartIndex, filteredTrays.Count());
    }

    private static IEnumerable<SampleTray> FilterByTrayIdAndLocation(IEnumerable<SampleTray> sampleTrays, GetSampleTrayListQuery request)
    {
        if (!string.IsNullOrEmpty(request.TrayId))
        {
            sampleTrays = sampleTrays.Where(x => x.Id.StartsWith(request.TrayId));
        }

        if (request.TrayLocation is { Count: > 0 })
        {
            sampleTrays = sampleTrays.Where(x => request.TrayLocation.Any(name => (string.IsNullOrEmpty(name) && x.InstrumentNumber == null) ||
                                                                                  (!string.IsNullOrEmpty(name) && x.InstrumentNumber == name)));
        }

        return sampleTrays;
    }

    private static IEnumerable<SampleTray> FilterByError(IEnumerable<SampleTray> sampleTrays, GetSampleTrayListQuery request)
    {
        if (!request.HasTubeIssues)
        {
            return sampleTrays;
        }
        return sampleTrays.Where(x => x.TubeErrorCount > 0);
    }

    private static IEnumerable<SampleTrayDto> SortTrays(IEnumerable<SampleTrayDto> sampleTrays, string? sortProperty, bool ascending)
    {
        return sortProperty switch
        {
            nameof(SampleTrayDto.Id) => ascending ? sampleTrays.OrderBy(x => x.Id) : sampleTrays.OrderByDescending(x => x.Id),
            nameof(SampleTrayDto.InstrumentName) => ascending ? sampleTrays.OrderBy(x => x.InstrumentName) : sampleTrays.OrderByDescending(x => x.InstrumentName),
            nameof(SampleTrayDto.ErrorCount) => ascending ? sampleTrays.OrderBy(x => x.ErrorCount) : sampleTrays.OrderByDescending(x => x.ErrorCount),
            nameof(SampleTrayDto.FreePositions) => ascending ? sampleTrays.OrderBy(x => x.FreePositions) : sampleTrays.OrderByDescending(x => x.FreePositions),
            _ => sampleTrays.OrderBy(x => x.Id)
        };
    }

    private string GetInstrumentName(string? instrumentNumber)
    {
        if (string.IsNullOrEmpty(instrumentNumber))
        {
            return Translations.TrayLocation_NotLoaded;
        }
        var instrumentName = instrumentManager.FindInstrument(instrumentNumber)?.DisplayName;
        return string.IsNullOrEmpty(instrumentName) ? Translations.TrayLocation_NotLoaded : instrumentName;
    }
}
