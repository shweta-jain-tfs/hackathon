using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.TaskCard.Application.Queries;

public class GetAllTaskCardsListQuery : SortedPartialQuery<TaskCardListItemDto>
{
    public List<string>? SelectedInstruments { get; set; }
}

internal class GetAllTaskCardsListQueryHandler(ITaskCardService taskCardService, IInstrumentManager instrumentManager)
    : IRequestHandler<GetAllTaskCardsListQuery, PartialQueryResult<TaskCardListItemDto>>
{
    public Task<PartialQueryResult<TaskCardListItemDto>> Handle(GetAllTaskCardsListQuery request, CancellationToken cancellationToken)
    {
        var instruments = instrumentManager.GetInstruments();
        var instrumentMap = instruments.ToDictionary(i => i.SerialNumber, i => i.DisplayName);
        var taskCardsList = new List<TaskCardListItemDto>();

        var taskCards = taskCardService.GetTaskCardsForInstruments(instrumentMap.Keys.ToList());

        if (request.SelectedInstruments is { Count: > 0 })
        {
            var selectedSerialNumbers = request.SelectedInstruments.ToHashSet();
            taskCards = taskCards.Where(tc => selectedSerialNumbers.Contains(tc.InstrumentNumber ?? string.Empty)).ToList();
        }

        foreach (var taskCard in taskCards)
        {
            taskCardsList.Add(new TaskCardListItemDto
            {
                Id = taskCard.Id,
                CardType = taskCard.CardType == TaskCardType.SystemError ? TaskCardType.Error : taskCard.CardType,
                ExpiresAt = taskCard.ExpiresAt,
                InstrumentNumber = taskCard.InstrumentNumber,
                InstrumentDisplayName = instrumentMap.GetValueOrDefault(taskCard.InstrumentNumber ?? string.Empty),
                CardState = taskCard.CurrentCardState
            });
        }

        // Apply default or Property Sorting
        taskCardsList = ApplySorting(taskCardsList, request.SortByProperty, request.SortByAscending);

        // Pagination
        var totalCount = taskCardsList.Count;
        var pagedTaskCardsList = taskCardsList
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        return Task.FromResult(new PartialQueryResult<TaskCardListItemDto>(
            pagedTaskCardsList,
            request.StartIndex,
            totalCount
        ));
    }

    private static List<TaskCardListItemDto> ApplySorting(List<TaskCardListItemDto> items, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            //default sort by multiple properties
            return items
                .OrderBy(x => x.ExpiresAt)
                .ThenBy(x => x.CardType)
                .ThenBy(x => x.InstrumentDisplayName)
                .ToList();
        }

        //sort by the specified property
        return sortProperty switch
        {
            nameof(TaskCardListItemDto.ExpiresAt) => ascending
                ? items.OrderBy(x => x.ExpiresAt).ToList()
                : items.OrderByDescending(x => x.ExpiresAt).ToList(),
            nameof(TaskCardListItemDto.CardType) => ascending
                ? items.OrderBy(x => x.CardType).ToList()
                : items.OrderByDescending(x => x.CardType).ToList(),
            nameof(TaskCardListItemDto.InstrumentDisplayName) => ascending
                ? items.OrderBy(x => x.InstrumentDisplayName).ToList()
                : items.OrderByDescending(x => x.InstrumentDisplayName).ToList(),
            _ => items
        };
    }
}
