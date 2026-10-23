namespace Pharos.Client.App.Result.Models;

public interface ISampleTestResultDataService
{
    Task<int> ApproveTestResults(ICollection<Guid> testOrderIds, bool reject = false, bool retest = false, int? instrumentDilution = null);
    Task<bool> GetTestResultsPendingReflex(int analyticalRunId);
    Task<ICollection<PendingReflex>> GetPendingReflexes(ICollection<Guid> testOrderIds);
    Task HandlePendingReflexes(int[] ids, bool doNotAddReflexTests = false);
    Task<bool> HasManualReflexDefinitionsInRequestCopy(int analyticalRunId);
    Task<MethodAndInstrumentName> GetMethodNameForAnalyticalRun(int analyticalRunId);
    Task<StatusType> GetOverallStatusForTestResults(int analyticalRunId);
    Task<bool> HasManualReflexDefinitions();
    Task<int> ResetTestResults(List<Guid> testOrderIds);
    Task UpdateAsync(SampleTestResult value);
    Task<SampleTestResult> GetSampleTestResult(int testResultId);
}