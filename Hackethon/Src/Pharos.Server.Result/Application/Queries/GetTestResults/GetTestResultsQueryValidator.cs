using Pharos.Server.Application.Queries;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

internal class GetTestResultsQueryValidator : SortedPartialQueryValidator<GetTestResultsQuery, AnalyticalRunTestResultDto>
{
    protected override string[] GetAllowedSortProperties()
    {
        return
        [
            nameof(AnalyticalRunTestResultDto.MeasurementDateTime),
            nameof(AnalyticalRunTestResultDto.SampleId),
            nameof(AnalyticalRunTestResultDto.ResultType),
            nameof(AnalyticalRunTestResultDto.TestName),
            nameof(AnalyticalRunTestResultDto.Status),
            nameof(AnalyticalRunTestResultDto.MeanConcentrationAsText),
            nameof(AnalyticalRunTestResultDto.TotalDilution),
            nameof(AnalyticalRunTestResultDto.MeanResponseAsText),
            nameof(AnalyticalRunTestResultDto.Class),
            nameof(AnalyticalRunTestResultDto.CutOff),
            nameof(AnalyticalRunTestResultDto.MeanQuotientAsText)
        ];
    }

    protected override bool UseCount(GetTestResultsQuery query)
    {
        return !query.BypassRowLimit;
    }
}
