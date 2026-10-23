using System.ComponentModel.DataAnnotations;
using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

public class GetInternalDilutionPlatesQuery : SortedPartialQuery<InternalDilutionPlateDto>, IRequest<PartialQueryResult<InternalDilutionPlatesResultDto>>
{
    public List<string>? SelectedInstruments { get; set; }
    public bool BypassRowLimit { get; set; }
}

internal class GetInternalDilutionPlatesQueryHandler(IInstrumentManager instrumentManager, IInstrumentReagentStorage instrumentReagentStorage)
    : IRequestHandler<GetInternalDilutionPlatesQuery, PartialQueryResult<InternalDilutionPlatesResultDto>>
{
    public Task<PartialQueryResult<InternalDilutionPlatesResultDto>> Handle(GetInternalDilutionPlatesQuery request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();
        var internalDilutionPlatesListDto = new List<InternalDilutionPlateDto>();

        // Filter instruments based on SelectedInstruments, or use all if null/empty
        var filteredInstruments = request.SelectedInstruments != null && request.SelectedInstruments.Count > 0
            ? instruments.Where(i => request.SelectedInstruments.Contains(i.DisplayName ?? "")).ToList()
            : instruments;

        foreach (var instrument in filteredInstruments)
        {
            var internalDilutionPlates = instrumentReagentStorage.GetDilutionPlatesStation(instrument.SerialNumber);

            foreach (var dilutionPlate in internalDilutionPlates)
            {
                internalDilutionPlatesListDto.Add(new InternalDilutionPlateDto
                {
                    Id = HashCode.Combine(dilutionPlate.Position, dilutionPlate.Cells.Count, instrument.DisplayName),
                    Position = GetPosition(dilutionPlate),
                    FreeWells = Constants.MaxDilutionPlateWells - dilutionPlate.Cells.Count,
                    Instrument = instrument.DisplayName ?? "",
                    InstrumentNumber = instrument.SerialNumber
                });
            }
        }

        // Apply default or Property Sorting
        internalDilutionPlatesListDto = ApplySorting(internalDilutionPlatesListDto, request.SortByProperty, request.SortByAscending);

        // Pagination
        var totalCount = internalDilutionPlatesListDto.Count;
        var pagedDilutionPlatesList = internalDilutionPlatesListDto
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();


        var internalDilutionPlateResult = new InternalDilutionPlatesResultDto
        {
            Items = pagedDilutionPlatesList,
            PlatesCount = pagedDilutionPlatesList.Count,
            FreeWellsCount = pagedDilutionPlatesList.Sum(x => x.FreeWells)
        };

        return Task.FromResult(new PartialQueryResult<InternalDilutionPlatesResultDto>(
            new List<InternalDilutionPlatesResultDto> { internalDilutionPlateResult },
            request.StartIndex,
            totalCount
        ));
    }

    private static InternalDilutionPlatePosition GetPosition(DilutionPlateInStation dilutionPlateInStation)
    {
        return dilutionPlateInStation.Position switch
        {
            1 => InternalDilutionPlatePosition.Left,
            2 => InternalDilutionPlatePosition.Right,
            _ => throw new ValidationException($"{nameof(DilutionPlateInStation)}.{nameof(DilutionPlateInStation.Position)} value is unknown.")
        };
    }

    private static List<InternalDilutionPlateDto> ApplySorting(List<InternalDilutionPlateDto> items, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            //default sort by multiple properties
            return items
                .OrderBy(x => x.Instrument)
                .ThenBy(x => x.Position)
                .ToList();
        }

        //sort by the specified property
        return sortProperty switch
        {
            nameof(InternalDilutionPlateDto.Position) => ascending
                ? items.OrderBy(x => x.Position).ToList()
                : items.OrderByDescending(x => x.Position).ToList(),
            nameof(InternalDilutionPlateDto.FreeWells) => ascending
                ? items.OrderBy(x => x.FreeWells).ToList()
                : items.OrderByDescending(x => x.FreeWells).ToList(),
            nameof(InternalDilutionPlateDto.Instrument) => ascending
                ? items.OrderBy(x => x.Instrument).ToList()
                : items.OrderByDescending(x => x.Instrument).ToList(),
            _ => items
        };
    }
}
