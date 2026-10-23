namespace Pharos.Client.App.Request.Models;

public interface IRequestSummaryDataService : IPagedDataSource<RequestSummary>, ISupportsRowLimitBypass
{
    Task<DeleteResponse> DeleteAsync(int[] entityIds);
}

internal sealed class RequestSummaryDataService : IRequestSummaryDataService
{
    private readonly HttpClient _httpClient;

    public RequestSummaryDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        var client = new RequestsClient(_httpClient);
        return await client.DeleteRequestAsync(entityIds);
    }

    public async Task<DataSourceResult<RequestSummary>> GetAsync(DataSourceRequest request)
    {
        string? filter = null;
        List<RequestState>? statuses = null;
        DateTimeOffset? fromSampleDate = null, toSampleDate = null;
        var bypassRowLimit = request.RequiresRowLimitBypass;
        var onHold = false;
        List<RequestIssueType>? issues = null;

        SetFilter(request, ref statuses, ref fromSampleDate, ref toSampleDate, ref filter, ref onHold, ref issues);

        var client = new RequestsClient(_httpClient);

        var result = await client.GetRequestsAsync(filter, fromSampleDate, toSampleDate, statuses, onHold, issues, bypassRowLimit, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<RequestSummary>(result.Items.ToList(), result.TotalCount);
    }

    private static void SetFilter(DataSourceFilterRequest request, ref List<RequestState>? statuses, ref DateTimeOffset? fromSampleDate, ref DateTimeOffset? toSampleDate, ref string? filter, ref bool onHold, ref List<RequestIssueType>? issues)
    {
        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(RequestSummary.SampleCollectionDate))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.GreaterThanOrEqualTo:
                        fromSampleDate = f.FilterValue as DateTimeOffset?;
                        continue;

                    case FilterOperator.LessThanOrEqualTo:
                        toSampleDate = f.FilterValue as DateTimeOffset?;
                        continue;
                }
            }

            if (f.PropertyName == nameof(RequestSummary.State))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        var items = f.FilterValues.Cast<RequestState>().ToList();
                        statuses ??= [];
                        statuses.AddRange(items);
                        continue;
                }
            }

            if (f.PropertyName == nameof(RequestSummary.SampleId))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        filter = f.FilterValue as string;
                        continue;
                }
            }

            if (f.PropertyName == nameof(RequestSummary.HasOnHoldTests))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        onHold = f.FilterValue is true;
                        continue;
                }
            }

            if(f.PropertyName == nameof(RequestSummary.IssueTemp))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        var items = f.FilterValues.Cast<RequestIssueType>().ToList();
                        issues ??= [];
                        issues.AddRange(items);
                        continue;
                }
            }

            throw new NotSupportedException(
                $"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }
}