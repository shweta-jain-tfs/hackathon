namespace Pharos.Client.App.Result.Models;

internal interface IAnalyticalRunDataSource : IEntityDataSource<AnalyticalRun>, ISupportsRowLimitBypass
{
    Task DeleteAnalyticalRuns(IEnumerable<int> analyticalRunIds);
}

internal sealed class AnalyticalRunDataSource(HttpClient httpClient) : IAnalyticalRunDataSource
{
    public async Task<DataSourceResult<AnalyticalRun>> GetAsync(DataSourceRequest request)
    {
        var client = new TestResultsClient(httpClient);
        List<StateStatus> statuses = [];
        List<string> instrumentNumbers = [];
        DateTimeOffset? fromStartDate = null, toStartDate = null;
        List<IssueStatus> issues = [];
        Filter(request, ref statuses, instrumentNumbers, ref fromStartDate, ref toStartDate, ref issues);

        var analyticalRuns = await client.GetAnalyticalRunsAsync(
            statuses,
            instrumentNumbers,
            fromStartDate,
            toStartDate,
            issues,
            request.RequiresRowLimitBypass,
            request.StartIndex,
            request.Count,
            request.SortByProperty,
            request.SortByAscending);

        return new DataSourceResult<AnalyticalRun>(analyticalRuns.Items.ToList(), analyticalRuns.TotalCount);
    }

    public async Task DeleteAnalyticalRuns(IEnumerable<int> analyticalRunIds)
    {
        var client = new TestResultsClient(httpClient);
        await client.DeleteAnalyticalRunsAsync(new DeleteAnalyticalRunsCommand
        {
            AnalyticalRunIds = analyticalRunIds.ToList()
        });
    }

    private static void Filter(DataSourceFilterRequest request, ref List<StateStatus> statuses, List<string> instrumentNumbers, ref DateTimeOffset? fromStartDate, ref DateTimeOffset? toStartDate, ref List<IssueStatus> issues)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(AnalyticalRun.StateStatus))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        var items = f.FilterValues.Cast<StateStatus>().ToList();
                        statuses.AddRange(items);
                        continue;
                }
            }

            if (f.PropertyName == nameof(AnalyticalRun.InstrumentName))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:

                        instrumentNumbers.Add(f.FilterValue as string ?? string.Empty);
                        continue;
                }
            }

            if (f.PropertyName == nameof(AnalyticalRun.StartDateTime))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.GreaterThanOrEqualTo:
                        fromStartDate = f.FilterValue as DateTimeOffset?;
                        continue;
                    case FilterOperator.LessThanOrEqualTo:
                        toStartDate = f.FilterValue as DateTimeOffset?;
                        continue;
                }
            }

            if (f.PropertyName == "IssueStatus")
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        issues.AddRange(f.FilterValues.Cast<IssueStatus>());
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    public async Task<AnalyticalRun> GetByIdAsync(int entityId)
    {
        var client = new TestResultsClient(httpClient);
        return await client.GetAnalyticalRunAsync(entityId);
    }
}
