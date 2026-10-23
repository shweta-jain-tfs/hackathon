namespace Pharos.Client.App.Result.Models;

internal sealed class TestResultDataService : ISampleTestResultDataService, ITestResultReplicateDataService, IAnalyticalRunResultDataSource, IQualityControlDataSource, ISupportsRowLimitBypass
{
    private readonly HttpClient _httpClient;

    public TestResultDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region AnalyticalRunResult
    public async Task<DataSourceResult<AnalyticalRunTestResult>> GetAsync(DataSourceRequest request, int analyticalRunId)
    {
        var client = new TestResultsClient(_httpClient);

        List<TestResultStatus>? testResultStatuses = null;
        string? identity = null;
        List<TestResultType>? resultTypes = null;

        Filter(request, ref testResultStatuses, ref identity, ref resultTypes);

        var result = await client.GetTestResultsAsync(
            analyticalRunId,
            testResultStatuses,
            identity,
            request.RequiresRowLimitBypass,
            resultTypes,
            request.StartIndex,
            request.Count ?? 0,
            request.SortByProperty,
            request.SortByAscending);

        return new DataSourceResult<AnalyticalRunTestResult>(result.Items.ToList(), result.TotalCount);
    }

    public async Task<int> GetTestResultIndexAsync(Guid testOrderId, int analyticalRunId, DataSourceRequest request)
    {
        var client = new TestResultsClient(_httpClient);

        List<TestResultStatus>? testResultStatuses = null;
        string? identity = null;
        List<TestResultType>? resultTypes = null;

        Filter(request, ref testResultStatuses, ref identity, ref resultTypes);

        var index = await client.GetTestResultIndexAsync(
            testOrderId,
            analyticalRunId,
            testResultStatuses,
            identity,
            resultTypes,
            request.SortByProperty,
            request.SortByAscending);

        return index;
    }
    #endregion

    #region SampleResult

    public async Task<MethodAndInstrumentName> GetMethodNameForAnalyticalRun(int analyticalRunId)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.GetMethodNameByRunAsync(analyticalRunId);
    }

    public async Task<StatusType> GetOverallStatusForTestResults(int analyticalRunId)
    {
        var client = new TestResultsClient(_httpClient);
        var status = await client.GetOverallStatusForTestResultsAsync(analyticalRunId);

        return status switch
        {
            TestResultSummaryStatus.Information => StatusType.Info,
            TestResultSummaryStatus.Warning => StatusType.Warning,
            TestResultSummaryStatus.NoSymbol => StatusType.None,
            _ => StatusType.None
        };
    }

    public async Task<ICollection<TestReplicateDetail>> GetTestReplicateDetails(int[] ids)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.GetTestReplicateDetailsAsync(ids);
    }

    public async Task<bool> HasManualReflexDefinitions()
    {
        var client = new ReflexClient(_httpClient);
        return await client.HasManualReflexDefinitionsAsync();
    }

    public async Task<int> ApproveTestResults(ICollection<Guid> testOrderIds, bool reject = false, bool retest = false, int? instrumentDilution = null)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.ApproveTestResultsAsync(new ApproveTestResultsCommand
        {
            TestOrderIds = testOrderIds,
            Reject = reject,
            Retest = retest,
            InstrumentDilution = instrumentDilution
        });
    }

    public async Task HandlePendingReflexes(int[] ids, bool doNotAddReflexTests = false)
    {
        var client = new RequestsClient(_httpClient);
        await client.HandlePendingReflexesAsync(new HandlePendingReflexesCommand
        {
            Ids = ids,
            DoNotAddReflexTests = doNotAddReflexTests
        });
    }

    public async Task<bool> GetTestResultsPendingReflex(int analyticalRunId)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.GetTestResultsPendingReflexAsync(analyticalRunId);
    }

    public async Task<ICollection<PendingReflex>> GetPendingReflexes(ICollection<Guid> testOrderIds)
    {
        var client = new RequestsClient(_httpClient);
        return await client.GetPendingReflexesAsync(testOrderIds);
    }

    public async Task<bool> HasManualReflexDefinitionsInRequestCopy(int analyticalRunId)
    {
        var client = new RequestsClient(_httpClient);
        return await client.HasManualReflexDefinitionsAsync(analyticalRunId);
    }

    public async Task UpdateAsync(SampleTestResult value)
    {
        var client = new TestResultsClient(_httpClient);

        await client.UpdateTestResultAsync(new UpdateSampleTestResultCommand
        {
            Id = value.Id,
            Comment = value.Comment,
            Replicates = value.Replicates.Select(x => new ActiveSampleTestReplicate
            {
                Id = x.Id,
                Active = x.Active
            }).ToList()
        });
    }

    public async Task<int> ResetTestResults(List<Guid> testOrderIds)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.ResetTestResultsAsync(new ResetTestResultsCommand
        {
            TestOrderIds = testOrderIds
        });
    }

    public async Task<SampleTestResult> GetSampleTestResult(int testResultId)
    {
        var client = new TestResultsClient(_httpClient);
        return await client.GetSampleTestResultAsync(testResultId);
    }
    #endregion

    #region QualityControlResult

    public int AnalyticalRunId { get; set; }

    public async Task<DataSourceResult<QualityControlTestResult>> GetAsync(DataSourceRequest request)
    {
        var client = new TestResultsClient(_httpClient);
        var result = await client.GetQualityControlByArAsync(AnalyticalRunId);
        return new DataSourceResult<QualityControlTestResult>(result.ToList(), result.Count);
    }

    public async Task<QualityControlChart> GetQualityControls(int analyticalRunId, string qualityControlName, string testName, int points)
    {
        return await new TestResultsClient(_httpClient).GetQualityControlForChartsAsync(analyticalRunId, qualityControlName, testName, points);
    }

    public Task<QualityControlTestResult> GetByIdAsync(int entityId)
    {
        var client = new TestResultsClient(_httpClient);
        return client.GetQualityControlByIdAsync(entityId);
    }

    public async Task UpdateQualityControlAsync(QualityControlTestResult value)
    {
        var client = new TestResultsClient(_httpClient);
        await client.UpdateQualityControlCommentAsync(new UpdateQualityControlCommentCommand
        {
            Id = value.Id,
            Comment = value.Comment
        });
    }
    #endregion

    private static void Filter(DataSourceFilterRequest request, ref List<TestResultStatus>? testResultStatuses, ref string? identity, ref List<TestResultType>? resultTypes)
    {
        // Check if Filters is null before iterating
        if (request.Filters == null)
        {
            return;
        }

        foreach (var f in request.Filters)
        {
            if (f.PropertyName == nameof(AnalyticalRunTestResult.Status))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:

                        testResultStatuses ??= [];

                        foreach (var filter in f.FilterValues)
                        {
                            if (Enum.TryParse(filter as string, out TestResultStatus status))
                            {
                                testResultStatuses.Add(status);
                            }
                        }

                        continue;
                }
            }

            if (f.PropertyName == nameof(AnalyticalRunTestResult.SampleId))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.Contains:
                        identity = f.FilterValue as string;
                        continue;
                }
            }

            if (f.PropertyName == nameof(AnalyticalRunTestResult.ResultType))
            {
                switch (f.FilterOperator)
                {
                    case FilterOperator.EqualsTo:
                        resultTypes ??= new List<TestResultType>();
                        foreach (var filter in f.FilterValues)
                        {
                            if (Enum.TryParse(filter as string, out TestResultType type))
                            {
                                resultTypes.Add(type);
                            }
                        }
                        continue;
                }
            }

            throw new NotSupportedException($"The specified filter is not supported by:  {nameof(DataSourceFilter.PropertyName)}={f.PropertyName}, {nameof(DataSourceFilter.FilterOperator)} = {f.FilterOperator}");
        }
    }

    // Do not use this method directly, use the GetAsync method with the analyticalRunId parameter instead.
    Task<DataSourceResult<AnalyticalRunTestResult>> IPagedDataSource<AnalyticalRunTestResult, int>.GetAsync(DataSourceRequest request)
    {
        throw new NotImplementedException("Use GetAsync(DataSourceRequest request, int analyticalRunId) instead.");
    }
}