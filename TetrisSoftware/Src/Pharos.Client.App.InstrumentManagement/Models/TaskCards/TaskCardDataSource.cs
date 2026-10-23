using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;

namespace Pharos.Client.App.InstrumentManagement.Models.TaskCards;

public class TaskCardDataSource : IPagedDataSource<TaskCardListItem>
{
    private readonly HttpClient _httpClient;

    public TaskCardDataSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<TaskCardListItem>> GetAsync(DataSourceRequest request)
    {
        var client = new TaskCardClient(_httpClient);
        List<string> selectedSerialNumbers = [];
        Filter(request, selectedSerialNumbers);
        var result = await client.GetAllTaskCardsListAsync(
            selectedSerialNumbers,
            request.StartIndex,
            request.Count,
            request.SortByProperty,
            request.SortByAscending,
            request.CancellationToken
        );
        return new DataSourceResult<TaskCardListItem>(result.Items.ToList(), result.TotalCount);
    }

    private static void Filter(DataSourceFilterRequest request, List<string> selectedSerialNumbers)
    {
        foreach (var f in request.Filters)
        {

            if (f.PropertyName == nameof(TaskCardListItem.InstrumentDisplayName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        selectedSerialNumbers.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }
}
