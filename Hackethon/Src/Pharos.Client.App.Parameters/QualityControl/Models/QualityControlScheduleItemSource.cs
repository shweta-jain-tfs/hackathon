namespace Pharos.Client.App.Parameters.QualityControl.Models;

public interface IQualityControlScheduleItemSource : IPagedDataSource<QualityControlSchedule>
{
    Task Update(UpdateQualityControlScheduleCommand command);
}

public class QualityControlScheduleItemSource : IQualityControlScheduleItemSource
{
    private readonly HttpClient _httpClient;

    public QualityControlScheduleItemSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DataSourceResult<QualityControlSchedule>> GetAsync(DataSourceRequest request)
    {
        var client = new QualityControlsClient(_httpClient);
        List<string> methods = [];
        SetFilter(ref methods, request.Filters);
        var result = await client.GetQualityControlSchedulesAsync(methods, request.StartIndex, request.Count, request.SortByProperty, request.SortByAscending);
        return new DataSourceResult<QualityControlSchedule>(result.Items.ToList(), result.TotalCount, result.GroupedCount);
    }

    private static void SetFilter(ref List<string> methods, ICollection<DataSourceFilter> requestFilters)
    {
        foreach (var f in requestFilters)
        {
            switch (f.PropertyName)
            {
                case nameof(QualityControlSchedule.MethodName):
                    switch (f.FilterOperator)
                    {
                        case FilterOperator.EqualsTo:
                            methods?.Add(f.FilterValue as string ?? string.Empty);
                            continue;
                    }

                    break;
            }
        }
    }

    public async Task Update(UpdateQualityControlScheduleCommand command)
    {
        var client = new QualityControlsClient(_httpClient);
        await client.UpdateQualityControlScheduleAsync(command.Id, command);
    }
}