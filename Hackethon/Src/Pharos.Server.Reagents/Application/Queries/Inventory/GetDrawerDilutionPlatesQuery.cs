using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain;
using Pharos.Server.Reagents.Application.Interfaces;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Reagents.Application.Queries.Inventory;

public class GetDrawerDilutionPlatesQuery : SortedPartialQuery<DrawerDilutionPlateDto>, IRequest<PartialQueryResult<DrawerDilutionPlatesResultDto>>
{
    public List<string>? SelectedInstruments { get; set; }
}

internal class GetDrawerDilutionPlatesQueryHandler(IInstrumentManager instrumentManager, IInstrumentReagentStorage instrumentReagentStorage)
    : IRequestHandler<GetDrawerDilutionPlatesQuery, PartialQueryResult<DrawerDilutionPlatesResultDto>>
{
    public Task<PartialQueryResult<DrawerDilutionPlatesResultDto>> Handle(GetDrawerDilutionPlatesQuery request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();
        var dilutionPlateStacksListDto = new List<DrawerDilutionPlateDto>();

        // Filter instruments based on SelectedInstruments, or use all if null/empty
        var filteredInstruments = request.SelectedInstruments != null && request.SelectedInstruments.Count > 0
            ? instruments.Where(i => request.SelectedInstruments.Contains(i.DisplayName ?? "")).ToList()
            : instruments;

        foreach (var instrument in filteredInstruments)
        {
            var dilutionPlateStacks = instrumentReagentStorage.GetDilutionPlatesDrawer(instrument.SerialNumber)
                .Where(dp => dp.NumberOfPlates > 0 || HasError(dp.Error));

            foreach (var stack in dilutionPlateStacks)
            {
                dilutionPlateStacksListDto.Add(new DrawerDilutionPlateDto
                {
                    Id = HashCode.Combine(stack.Position, stack.NumberOfPlates, stack.Error, instrument.DisplayName),
                    DrawerPosition = stack.Position,
                    DilutionPlates = stack.NumberOfPlates,
                    Status = GetStatus(stack),
                    Instrument = instrument.DisplayName ?? ""
                });
            }
        }

        // Apply default or Property Sorting
        dilutionPlateStacksListDto = ApplySorting(dilutionPlateStacksListDto, request.SortByProperty, request.SortByAscending);

        // Pagination
        var totalCount = dilutionPlateStacksListDto.Count;
        var pagedDilutionPlateStacksList = dilutionPlateStacksListDto
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        var platesCount = pagedDilutionPlateStacksList.Sum(x => x.DilutionPlates);

        var dilutionPlateStackResult = new DrawerDilutionPlatesResultDto
        {
            Id = HashCode.Combine(dilutionPlateStacksListDto.Count, totalCount),
            Items = pagedDilutionPlateStacksList,
            PlatesCount = platesCount,
            WellsCount = platesCount * Constants.MaxDilutionPlateWells
        };

        return Task.FromResult(new PartialQueryResult<DrawerDilutionPlatesResultDto>(
            new List<DrawerDilutionPlatesResultDto> { dilutionPlateStackResult },
            request.StartIndex,
            totalCount
        ));
    }

    private static bool HasError(EnumDilutionPlatePositionError error)
    {
        return error == EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates
            || error == EnumDilutionPlatePositionError.CouldNotPickupDilutionPlates;
    }

    private static ReagentPositionStatus GetStatus(DilutionPlateStack dilutionPlateStack)
    {
        return dilutionPlateStack.Error switch
        {
            EnumDilutionPlatePositionError.CouldNotDetectDilutionPlates => ReagentPositionStatus.DilutionPlateNotDetected,
            EnumDilutionPlatePositionError.CouldNotPickupDilutionPlates => ReagentPositionStatus.DilutionPlateNotPickedUp,
            _ => ReagentPositionStatus.Available
        };
    }

    private static List<DrawerDilutionPlateDto> ApplySorting(List<DrawerDilutionPlateDto> items, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            //default sort by multiple properties
            return items
                .OrderBy(x => x.Instrument)
                .ThenBy(x => x.DrawerPosition)
                .ToList();
        }

        //sort by the specified property
        return sortProperty switch
        {
            nameof(DrawerDilutionPlateDto.DrawerPosition) => ascending
                ? items.OrderBy(x => x.DrawerPosition).ToList()
                : items.OrderByDescending(x => x.DrawerPosition).ToList(),
            nameof(DrawerDilutionPlateDto.DilutionPlates) => ascending
                ? items.OrderBy(x => x.DilutionPlates).ToList()
                : items.OrderByDescending(x => x.DilutionPlates).ToList(),
            nameof(DrawerDilutionPlateDto.Status) => ascending
                ? items.OrderBy(x => x.Status).ToList()
                : items.OrderByDescending(x => x.Status).ToList(),
            nameof(DrawerDilutionPlateDto.Instrument) => ascending
                ? items.OrderBy(x => x.Instrument).ToList()
                : items.OrderByDescending(x => x.Instrument).ToList(),
            _ => items
        };
    }
}