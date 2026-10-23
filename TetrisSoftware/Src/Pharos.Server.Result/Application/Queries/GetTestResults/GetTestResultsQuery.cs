using MediatR;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Queries.GetTestResults;

public class GetTestResultsQuery : SortedPartialQuery<AnalyticalRunTestResultDto>
{
    public int? AnalyticalRunId { get; set; }
    public TestResultStatus[]? TestResultStatus { get; set; }
    public string? Identity { get; set; }
    public bool BypassRowLimit { get; set; }
    public TestResultType[]? ResultTypes { get; set; }
}

internal class GetResultsQueryHandler(IResultDbContext context, IMethodsService methodsService, ISender sender, ISampleIdFormatter sampleIdFormatter) : PartialQueryHandlerBase<GetTestResultsQuery, TestResult, AnalyticalRunTestResultDto>, IRequestHandler<GetTestResultsQuery, PartialQueryResult<AnalyticalRunTestResultDto>>
{
    public async Task<PartialQueryResult<AnalyticalRunTestResultDto>> Handle(GetTestResultsQuery request, CancellationToken cancellationToken)
    {
        var query = context.TestResults.Where(x => x.ResultType == TestResultType.Sample || x.ResultType == TestResultType.QualityControl);

        var items = await GetPartialQueryResultAsync(query, request, cancellationToken);

        return items;
    }

    protected override async Task AttachExtraInformation(List<AnalyticalRunTestResultDto> items, CancellationToken cancellationToken)
    {
        foreach (var result in items)
        {
            // Fetch method test data.
            var methodTestData = await methodsService.GetMethodTest(result.MethodName, result.TestName, x => new
            {
                Unit = x.UserConcentrationUnit != null
                    ? x.UserConcentrationUnit.ConcentrationUnit
                    : x.DefaultConcentrationUnit.ConcentrationUnit,
                x.ReportConcentration,
                x.ReportCutOff,
                ReportQuotient = x.ReportQuotient != QuotientReportMode.NoReport,
                x.ReportClass
            });

            result.Unit = methodTestData?.Unit ?? string.Empty;

            if (!(methodTestData?.ReportConcentration ?? false))
            {
                result.MeanConcentrationAsText = null;
                result.MeanConcentrationRaw = null;
                result.Unit = string.Empty;
            }
            if (!(methodTestData?.ReportCutOff ?? false))
            {
                result.CutOff = null;
            }
            if (!(methodTestData?.ReportQuotient ?? false))
            {
                result.MeanQuotientAsText = null;
            }
            if (!(methodTestData?.ReportClass ?? false))
            {
                result.Class = null;
            }

            // Resolve the request using the configured Sample ID comparison.
            if (result is { SampleId: not null })
            {
                var requestUid = new RequestUid
                {
                    SampleId = result.SampleId,
                    RequestCreatedDate = result.RequestDate ?? DateTimeOffset.MinValue //TODO, can the date be null?
                };
                var request = await sender.Send(new GetRequestIdFromResultQuery(requestUid), cancellationToken).ConfigureAwait(false);

                if (request.RequestExists)
                {
                    result.RequestId = request.RequestId;
                    result.SampleId = request.SampleId ?? result.SampleId;
                    result.SampleIdDisplay = sampleIdFormatter.Format(result.SampleId, request.RequestCreatedDate);
                }
            }

        }
    }

    protected override IQueryable<TestResult> AddSorting(IQueryable<TestResult> query, string? sortProperty, bool ascending)
    {
        return TestResultSortingHelper.ApplySorting(query, sortProperty, ascending);
    }

    protected override IQueryable<AnalyticalRunTestResultDto> ApplyProjection(IQueryable<TestResult> query)
    {
        var result = query.Select(r => new AnalyticalRunTestResultDto
        {
            Id = r.Id,
            TestName = r.TestName,
            NumberOfReplicates = r.NumberOfReplicates,
            MethodName = r.MethodName,
            ResultType = r.ResultType,
            MeanResponseAsText = r.MeanResponseAsText,
            MeanConcentrationAsText = r.MeanConcentrationAsText,
            MeanConcentrationRaw = r is SampleTestResult ? ((SampleTestResult)r).MeanConcentrationRaw.ToString() : (r.MeanConcentration / r.PreDilutionFactor / r.InstrumentDilutionFactor).ToString(),
            TotalDilution = r.PreDilutionFactor * r.InstrumentDilutionFactor,
            Status = r.Status,
            MeasurementDateTime = r.MeasurementDateTime,
            Comment = r.OperatorComment,
            InstrumentDilution = r.InstrumentDilutionFactor,
            PreDilution = r.PreDilutionFactor,

            // Sample-only fields — null/default when the row is a QualityControlTestResult
            TestOrderId = r is SampleTestResult ? ((SampleTestResult)r).TestOrderId : null,
            ApprovalStatus = r is SampleTestResult ? ((SampleTestResult)r).ApprovalStatus : null,
            RequestDate = r is SampleTestResult ? ((SampleTestResult)r).RequestUid.RequestCreatedDate : null,

            // Identity: Sample ID for samples, QC name for QCs (same display slot)
            SampleId = r is SampleTestResult
            ? ((SampleTestResult)r).RequestUid.SampleId
            : ((QualityControlTestResult)r).QualityControlName,
            SampleIdDisplay = r is SampleTestResult
            ? sampleIdFormatter.Format(((SampleTestResult)r).RequestUid)
            : ((QualityControlTestResult)r).QualityControlName,

            // Fields that exist on both derived types (but not on the TestResult base) — branch per type
            Class = r is SampleTestResult ? ((SampleTestResult)r).MeanClass : ((QualityControlTestResult)r).MeanClass,
            CutOff = r is SampleTestResult ? ((SampleTestResult)r).MeanCutOff : ((QualityControlTestResult)r).MeanCutOff,
            MeanQuotientAsText = r is SampleTestResult ? ((SampleTestResult)r).MeanQuotientAsText : ((QualityControlTestResult)r).MeanQuotientAsText,
        });

        return result;
    }

    protected override IQueryable<TestResult> AddFiltering(IQueryable<TestResult> query, GetTestResultsQuery request)
    {
        if (request.AnalyticalRunId != null)
        {
            query = query.Where(x => x.AnalyticalRunId == request.AnalyticalRunId);
        }

        if (request.ResultTypes is { Length: > 0 })
        {
            query = query.Where(x => request.ResultTypes.Contains(x.ResultType));
        }

        if (request.Identity != null)
        {
            query = query.Where(x =>
                (x is SampleTestResult && ((SampleTestResult)x).RequestUid.SampleId.Contains(request.Identity)) ||
                (x is QualityControlTestResult && ((QualityControlTestResult)x).QualityControlName.Contains(request.Identity)));
        }

        if (request.TestResultStatus is { Length: > 0 })
        {
            query = query.Where(x => request.TestResultStatus.Contains(x.Status));
        }

        return query;
    }
}
