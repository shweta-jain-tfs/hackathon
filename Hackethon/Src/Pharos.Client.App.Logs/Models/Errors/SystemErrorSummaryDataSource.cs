namespace Pharos.Client.App.Logs.Models.Errors;

internal sealed class SystemErrorSummaryDataSource : IEntityDataSource<SystemErrorSummary>, ISupportsRowLimitBypass
{
    private readonly SystemErrorsClient _client;

    public SystemErrorSummaryDataSource(HttpClient httpClient)
    {
        _client = new SystemErrorsClient(httpClient);
    }

    /// <summary>
    /// Loads system errors for the grid request.
    /// </summary>
    public async Task<DataSourceResult<SystemErrorSummary>> GetAsync(DataSourceRequest request)
    {
        var errorCode = GetErrorCodeFilter(request);
        var unhandledOnly = GetUnhandledOnlyFilter(request);
        var (instrumentNumbers, includeSystemErrors) = GetInstrumentNumberFilter(request);
        var classifications = GetClassificationFilter(request);
        var result = await _client.GetSystemErrorsAsync(errorCode, unhandledOnly, includeSystemErrors, instrumentNumbers, classifications, request.StartIndex, request.Count ?? 0, request.SortByProperty, request.SortByAscending);

        return new DataSourceResult<SystemErrorSummary>(result.Items.ToList(), result.TotalCount);
    }

    /// <summary>
    /// Loads a single system error by id.
    /// </summary>
    public async Task<SystemErrorSummary> GetByIdAsync(int entityId)
    {
        return await _client.GetSystemErrorAsync(entityId);
    }

    /// <summary>
    /// Gets the error code text filter from the request.
    /// </summary>
    private static string? GetErrorCodeFilter(DataSourceRequest request)
    {
        foreach (var filter in request.Filters)
        {
            if (filter.PropertyName == nameof(SystemErrorSummary.Code)
                && filter.FilterOperator == FilterOperator.EqualsTo)
            {
                var value = filter.FilterValue?.ToString()?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the unhandled-only filter from the request.
    /// </summary>
    private static bool? GetUnhandledOnlyFilter(DataSourceRequest request)
    {
        foreach (var filter in request.Filters)
        {
            if (filter.PropertyName == nameof(SystemErrorSummary.IsUnhandled)
                && filter.FilterOperator == FilterOperator.Contains)
            {
                if (filter.FilterValue is bool boolValue)
                {
                    return boolValue ? true : null;
                }

                if (bool.TryParse(filter.FilterValue?.ToString(), out var parsed))
                {
                    return parsed ? true : null;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets selected instrument numbers from the request.
    /// </summary>
    private static (List<string>? InstrumentNumbers, bool? IncludeSystemErrors) GetInstrumentNumberFilter(DataSourceRequest request)
    {
        var selectedValues = request.Filters
            .Where(filter => filter.PropertyName == nameof(SystemErrorSummary.InstrumentName)
                && filter.FilterOperator == FilterOperator.EqualsTo)
            .SelectMany(filter => filter.FilterValues.Cast<object?>())
            .Select(value => value?.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct()
            .ToList();

        bool? includeSystemErrors = selectedValues.Contains(Pharos.Client.Common.Models.SystemErrorsFilterTokens.System) ? true : null;

        var instrumentNumbers = selectedValues
            .Where(value => value != Pharos.Client.Common.Models.SystemErrorsFilterTokens.System)
            .ToList();

        return (instrumentNumbers.Count > 0 ? instrumentNumbers : null, includeSystemErrors);
    }

    /// <summary>
    /// Gets selected classifications from the request.
    /// </summary>
    private static List<ErrorClassification>? GetClassificationFilter(DataSourceRequest request)
    {
        var classifications = request.Filters
            .Where(filter => filter.PropertyName == nameof(SystemErrorSummary.Classification)
                && filter.FilterOperator == FilterOperator.EqualsTo)
            .SelectMany(filter => filter.FilterValues.Cast<object?>())
            .Select(TryConvertClassification)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();

        return classifications.Count > 0 ? classifications : null;
    }

    /// <summary>
    /// Converts a filter value to an error classification.
    /// </summary>
    private static ErrorClassification? TryConvertClassification(object? value)
    {
        if (value is ErrorClassification classification)
        {
            return classification;
        }

        if (value is int intValue)
        {
            return (ErrorClassification)intValue;
        }

        if (Enum.TryParse<ErrorClassification>(value?.ToString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
