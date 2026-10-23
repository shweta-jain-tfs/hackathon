using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

/// <summary>
/// Sorting helper so that GetTestResultIndexQuery and GetSampleTestResultsQuery use the same sorting.
/// This is needed for the scrolling to scroll to the correct position.
/// </summary>
internal class TestResultSortingHelper
{
    internal static IQueryable<TestResult> ApplySorting(IQueryable<TestResult> query, string? sortProperty, bool ascending)
    {
        if (string.IsNullOrEmpty(sortProperty))
        {
            return query.OrderByDescending(x => x.MeasurementDateTime)
                        .ThenByDescending(x => x.Id);
        }

        query = sortProperty switch
        {
            nameof(AnalyticalRunTestResultDto.MeasurementDateTime) => ascending
                ? query.OrderBy(x => x.MeasurementDateTime).ThenBy(x => x.Id)
                : query.OrderByDescending(x => x.MeasurementDateTime).ThenByDescending(x => x.Id),

            nameof(AnalyticalRunTestResultDto.TestName) => ascending
                ? query.OrderBy(x => x is SampleTestResult ? ((SampleTestResult)x).TestNameNaturalSortKey : ((QualityControlTestResult)x).TestName)
                       .ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x is SampleTestResult ? ((SampleTestResult)x).TestNameNaturalSortKey : ((QualityControlTestResult)x).TestName)
                       .ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.SampleId) => ascending
                ? query.OrderBy(x => x is SampleTestResult ? ((SampleTestResult)x).SampleIdNaturalSortKey : ((QualityControlTestResult)x).QualityControlName)
                       .ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x is SampleTestResult ? ((SampleTestResult)x).SampleIdNaturalSortKey : ((QualityControlTestResult)x).QualityControlName)
                       .ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.ResultType) => ascending
                ? query.OrderBy(x => x.ResultType).ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x.ResultType).ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.Status) => ascending
                ? query.OrderBy(x => x.Status == TestResultStatus.Aborted ? 1
                        : x.Status == TestResultStatus.Flagged ? 2
                        : x.Status == TestResultStatus.PendingApproval ? 3
                        : x.Status == TestResultStatus.Processing ? 4
                        : x.Status == TestResultStatus.PendingCalibration ? 5
                        : x.Status == TestResultStatus.PendingBlank ? 6
                        : x.Status == TestResultStatus.Ready ? 7
                        : x.Status == TestResultStatus.Approved ? 8
                        : x.Status == TestResultStatus.Rejected ? 9
                        : x.Status == TestResultStatus.Reported ? 10
                        : 11)
                        .ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x.Status == TestResultStatus.Aborted ? 1
                        : x.Status == TestResultStatus.Flagged ? 2
                        : x.Status == TestResultStatus.PendingApproval ? 3
                        : x.Status == TestResultStatus.Processing ? 4
                        : x.Status == TestResultStatus.PendingCalibration ? 5
                        : x.Status == TestResultStatus.PendingBlank ? 6
                        : x.Status == TestResultStatus.Ready ? 7
                        : x.Status == TestResultStatus.Approved ? 8
                        : x.Status == TestResultStatus.Rejected ? 9
                        : x.Status == TestResultStatus.Reported ? 10
                        : 11)
                    .ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.MeanConcentrationAsText) => ascending
                ? query.OrderBy(x => x.MeanConcentration).ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x.MeanConcentration).ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.MeanResponseAsText) => ascending
                ? query.OrderBy(x => x.MeanResponse).ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x.MeanResponse).ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.TotalDilution) => ascending
                ? query.OrderBy(x => (x.PreDilutionFactor * x.InstrumentDilutionFactor)).ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => (x.PreDilutionFactor * x.InstrumentDilutionFactor)).ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.Class) => ascending
                ? query.OrderBy(x => x is SampleTestResult ? ((SampleTestResult)x).MeanClassNaturalSortKey : ((QualityControlTestResult)x).MeanClass)
                       .ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x is SampleTestResult ? ((SampleTestResult)x).MeanClassNaturalSortKey : ((QualityControlTestResult)x).MeanClass)
                       .ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.CutOff) => ascending
                ? query.OrderBy(x => x is SampleTestResult ? ((SampleTestResult)x).MeanCutOffNaturalSortKey : ((QualityControlTestResult)x).MeanCutOff)
                       .ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x is SampleTestResult ? ((SampleTestResult)x).MeanCutOffNaturalSortKey : ((QualityControlTestResult)x).MeanCutOff)
                       .ThenByDescending(x => x.MeasurementDateTime),

            nameof(AnalyticalRunTestResultDto.MeanQuotientAsText) => ascending
                ? query.OrderBy(x => x is SampleTestResult ? ((SampleTestResult)x).MeanQuotientNaturalSortKey : ((QualityControlTestResult)x).MeanQuotientAsText)
                       .ThenByDescending(x => x.MeasurementDateTime)
                : query.OrderByDescending(x => x is SampleTestResult ? ((SampleTestResult)x).MeanQuotientNaturalSortKey : ((QualityControlTestResult)x).MeanQuotientAsText)
                       .ThenByDescending(x => x.MeasurementDateTime),

            _ => query.OrderByDescending(x => x.MeasurementDateTime)
        };

        return query;
    }
}
