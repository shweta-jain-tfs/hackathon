namespace Pharos.Client.App.Logs.Models.AuditTrail;

internal class AuditTrailDataService(HttpClient httpClient) : IAuditTrailDataService, ISupportsRowLimitBypass
{
    private readonly AuditTrailClient _client = new(httpClient);

    public async Task<DataSourceResult<AuditTrailRecord>> GetAsync(DataSourceRequest request)
    {
        DateTimeOffset? fromDate = null, toDate = null;
        var searchText = string.Empty;
        var userNames = new List<string>();

        SetFilters(request, ref searchText, ref fromDate, ref toDate, userNames);

        var distinctUserNames = userNames
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var partialResult = await _client.GetAuditTrailRecordsAsync(
            searchText ?? string.Empty,
            distinctUserNames,
            fromDate,
            toDate,
            request.RequiresRowLimitBypass,
            request.StartIndex,
            request.Count,
            request.SortByProperty ?? string.Empty,
            request.SortByAscending
        );

        var items = partialResult.Items.ToList();

        return new DataSourceResult<AuditTrailRecord>(items, partialResult.TotalCount);
    }

    public async Task<string> ExportAuditTrailAsync(DataSourceRequest request)
    {
        DateTimeOffset? fromDate = null, toDate = null;
        var searchText = string.Empty;
        var userNames = new List<string>();

        SetFilters(request, ref searchText, ref fromDate, ref toDate, userNames);

        var query = new GetAuditTrailExportQuery
        {
            SearchText = searchText,
            UserNames = userNames,
            FromDate = fromDate,
            ToDate = toDate
        };

        return await _client.ExportAuditTrailRecordsAsync(query);
    }

    private static void SetFilters(DataSourceRequest request, ref string? searchText, ref DateTimeOffset? fromDate, ref DateTimeOffset? toDate, List<string> userNames)
    {
        foreach (var f in request.Filters)
        {
            // Map Search field and Activity (Description) filter to SearchText
            if (f is { FilterOperator: FilterOperator.Contains } &&
                (f.PropertyName == nameof(AuditTrailRecord.SearchText) ||
                 f.PropertyName == nameof(AuditTrailRecord.Description)))
            {
                searchText = f.FilterValue as string;
                continue;
            }

            if (f is { PropertyName: nameof(AuditTrailRecord.Created) })
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.GreaterThanOrEqualTo:
                        fromDate = f.FilterValue as DateTimeOffset?;
                        continue;
                    case FilterOperator.LessThanOrEqualTo:
                        toDate = f.FilterValue as DateTimeOffset?;
                        continue;
                }
            }

            if (f is { PropertyName: nameof(AuditTrailRecord.CreatedBy), FilterOperator: FilterOperator.EqualsTo })
            {
                foreach (var v in f.FilterValues)
                {
                    var s = v?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        userNames.Add(s);
                    }
                }
            }
        }
    }
    public async Task<List<string>> GetOperatorNamesAsync()
    {
        var operators = await _client.GetOperatorsAsync();
        return operators
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    #region Not used
    public Task<int> CreateAsync(AuditTrailRecord value)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteResponse> DeleteAsync(int[] entityIds)
    {
        throw new NotImplementedException();
    }

    public Task<AuditTrailRecord> GetByIdAsync(int entityId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(AuditTrailRecord value)
    {
        throw new NotImplementedException();
    }
    #endregion
}
