using System.Text;
using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Services.ResultExportService;

internal class ResultExportService(
    IResultDbContext dbContext,
    ICurrentUserService currentUserService,
    IPharosVersionService pharosVersionService,
    ISender sender,
    IInstrumentManager instrumentManager,
    IFileWriter fileWriter) : IResultExportService
{
    /// <summary>
    /// Create a result export file based on one or more analytical runs. If a custom run (CMR) was used to resolve the
    /// analytical run IDs, it can be supplied along with the analytical run IDs.
    /// </summary>
    /// <param name="analyticalRunIdList">IDs of analytical runs to include in the export.</param>
    /// <param name="cancellationToken">Token for cancelling async operation.</param>
    /// <param name="cmrRunIdUsed">Custom run ID to use in the information section and file name.</param>
    /// <returns>A stream and file name for the exported CSV file.</returns>
    public async Task<NamedStream> GenerateResultExportStream(IEnumerable<int> analyticalRunIdList, CancellationToken cancellationToken, string? cmrRunIdUsed = null)
    {
        var runIdList = analyticalRunIdList as IList<int> ?? analyticalRunIdList.ToList();

        if (cancellationToken.IsCancellationRequested)
        {
            return new NamedStream("", Stream.Null);
        }

        // these can not run in parallel
        var results = await GetResultSection(runIdList, cancellationToken);
        var information = await GetInformationSection(runIdList, cmrRunIdUsed, cancellationToken);
        var mean = await GetMeanSection(runIdList, cancellationToken);

        var csvContent = ResultExportCsvWriter.WriteCsv(information, results, mean);
        var fileName = ResultExportFileName.GenerateFileName(information);
        var csvFileStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        return new NamedStream(fileName, csvFileStream);
    }

    public async Task SaveResultExportStream(IEnumerable<int> analyticalRunIdList, string folder, CancellationToken cancellationToken, string? cmrRunIdUsed = null)
    {
        using var memoryStream = new MemoryStream();
        var csvFile = await GenerateResultExportStream(analyticalRunIdList, CancellationToken.None, cmrRunIdUsed);
        await csvFile.Stream.CopyToAsync(memoryStream, cancellationToken);
        await fileWriter.WriteAsync(csvFile.FileName, folder, memoryStream.ToArray(), CancellationToken.None);
    }

    /// <summary>
    /// Get the [INFORMATION] section from the database.
    /// </summary>
    /// <param name="analyticalRunIdList">ID of the analytical runs that were used to export the [RESULTS] section.</param>
    /// <param name="cancellationToken">Token for cancelling async operation.</param>
    /// <param name="cmrRunIdUsed">Any CMR custom run ID that was used to trigger the export.</param>
    /// <returns>Information section ready for CSV serialization.</returns>
    /// <exception cref="NotFoundException">If not matching analytical run was found in the database.</exception>
    internal async Task<CsvInformation> GetInformationSection(IEnumerable<int> analyticalRunIdList, string? cmrRunIdUsed, CancellationToken cancellationToken)
    {
        var query = dbContext.AnalyticalRuns
            .AsNoTracking()
            .Where(analyticalRun => analyticalRunIdList.Contains(analyticalRun.Id))
            .OrderBy(analyticalRun => analyticalRun.StartDateTime)
            .Select(analyticalRun => new
            {
                analyticalRun.InstrumentAnalyticalRunId,
                analyticalRun.CmrRunId,
                analyticalRun.MethodName,
                analyticalRun.StartDateTime,
                analyticalRun.EndDateTime,
                analyticalRun.InstrumentNumber
            });

        var runInfo = await query.ToListAsync(cancellationToken);

        if (runInfo.Count == 0 && !cancellationToken.IsCancellationRequested)
        {
            throw new NotFoundException("AnalyticalRun", analyticalRunIdList);
        }

        var isSingleRun = runInfo.Count == 1;
        var typeOfExport = GetExportType(cmrRunIdUsed, isSingleRun);
        var startTimes = runInfo.Select(run => run.StartDateTime).ToList();
        var endTimes = runInfo.Select(run => run.EndDateTime).ToList();
        var cmrRunIdToDisplay = cmrRunIdUsed ?? (runInfo.Select(x => x.CmrRunId).Distinct().Count() > 1 ? string.Empty : runInfo.Select(x => x.CmrRunId).FirstOrDefault());

        var hasMultipleInstruments = runInfo.Select(run => run.InstrumentNumber).Distinct().Count() > 1;

        var userName = await GetExportedByUserName(cancellationToken);

        var information = new CsvInformation
        {
            TypeOfExport = (int)typeOfExport,
            StartDateTime = startTimes.Min(),
            EndDateTime = endTimes.Contains(null) ? null : endTimes.Max(),
            AnalyticalRunId = isSingleRun ? runInfo.First().InstrumentAnalyticalRunId.ToString() : "",
            CustomRunIdentity = cmrRunIdToDisplay,
            PharosVersion = pharosVersionService.Version,
            ExportByUser = userName,
            MethodName = isSingleRun ? runInfo.First().MethodName : ""
        };

        if (!hasMultipleInstruments)
        {
            var instrument = instrumentManager.FindInstrument(runInfo.First().InstrumentNumber);

            information.InstrumentName = instrument?.DisplayName ?? "";
            information.InstrumentType = instrument == null ? null : (int)instrument.InstrumentType;
            information.InstrumentSerialNumber = runInfo.First().InstrumentNumber;
        }

        return information;
    }

    /// <summary>
    /// Get the [RESULTS] section from the test order replicates in the database.
    /// </summary>
    /// <param name="analyticalRunIdList">ID of the analytical runs to be included in the export.</param>
    /// <param name="cancellationToken">Token for cancelling async operation.</param>
    /// <returns>A list of CSV serializable items.</returns>
    internal async Task<List<CsvResult>> GetResultSection(IEnumerable<int> analyticalRunIdList, CancellationToken cancellationToken)
    {
        var testResults = await dbContext.TestResults
            .Where(x => x.AnalyticalRunId.HasValue && analyticalRunIdList.Contains(x.AnalyticalRunId.Value))
            .Include(x => x.AnalyticalRun)
            .Include(x => x.Replicates!)
            .ThenInclude(x => x.TestReplicateReagents)
            .AsSplitQuery()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var replicateIds = testResults.SelectMany(tr => tr.Replicates?.Select(r => r.Id) ?? []).ToList();

        await dbContext.TestReplicateErrors
            .Where(r => replicateIds.Contains(r.TestReplicateId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = new List<ResultExportResultData>();
        foreach (var testResult in testResults)
        {
            if (testResult.Replicates is not { Count: > 0 })
            {
                continue;
            }
            foreach (var replicate in testResult.Replicates)
            {
                results.Add(new ResultExportResultData
                {
                    InstrumentReplicateId = replicate.InstrumentReplicateId,
                    MeasurementDateTime = replicate.MeasurementDateTime,
                    IsAborted = replicate.IsAborted,
                    SampleId = (testResult as SampleTestResult)?.RequestUid.SampleId
                            ?? (testResult as CalibratorTestResult)?.CalibratorPointName
                            ?? (testResult as CurveControlTestResult)?.CurveControlPointName
                            ?? (testResult as QualityControlTestResult)?.QualityControlName
                            ?? "",
                    RequestCreatedDate = (testResult as SampleTestResult)?.RequestUid.RequestCreatedDate ?? DateTimeOffset.MinValue,
                    MethodName = testResult.MethodName,
                    TestName = testResult.TestName,
                    OrderType = testResult.ResultType,
                    Reagents = replicate.TestReplicateReagents?.OrderBy(r => r.SerialNumber).ToList() ?? [],
                    RU = replicate.Response,
                    LowRU = replicate.IsLowResponse,
                    Concentration = replicate.Concentration,
                    Quotient = (replicate as SampleTestReplicate)?.Quotient ?? (replicate as QualityControlTestReplicate)?.Quotient,
                    Class = (replicate as SampleTestReplicate)?.Class ?? "",
                    CutOff = (replicate as SampleTestReplicate)?.CutOff ?? "",
                    PreDilutionFactor = testResult.PreDilutionFactor,
                    InstrumentDilutionFactor = testResult.InstrumentDilutionFactor,
                    InstrumentAnalyticalRunId = testResult.AnalyticalRun!.InstrumentAnalyticalRunId,
                    Comment = testResult.OperatorComment,
                    FluorometerValue = replicate.FluorometerValue,
                    BlankId = replicate.UsedBlankId,
                    CmrRunId = testResult.AnalyticalRun.CmrRunId,
                    CmrTestId = replicate.CmrTestId,
                    CurveId = (testResult as CalibratorTestResult)?.CurveId ?? 0,
                    CalculationCurveId = testResult.AnalyticalRun.CurveId,
                    GroupId = (testResult as GroupTestResult)?.GroupId,
                    CalibratorOriginalResponse = (replicate as CalibratorTestReplicate)?.CalibratorOriginalResponse ?? 0,
                    EditedResponse = (replicate as CalibratorTestReplicate)?.EditedResponse,
                    Judgement = replicate is CalibratorTestReplicate ctr ? (int)ctr.NormDoseCheckResult
                                     : replicate is CurveControlTestReplicate ccr ? (int)ccr.Judgment
                                     : replicate is QualityControlTestReplicate ? (int)((QualityControlTestResult)testResult).Judgment
                                     : 0,
                    ErrorCodes = replicate.TestReplicateErrors?.Select(e => e.ErrorCode).ToArray() ?? [],
                    IrwRow = replicate.IrwRow,
                    IrwSpoke = replicate.IrwSpoke,
                    ErwRow = replicate.ErwRow,
                    ErwSpoke = replicate.ErwSpoke
                });
            }
        }

        // Test orders are stored in the different database context
        // To get RequestId, we need to run another query.
        var sampleResults = results.Where(x => x.OrderType == TestResultType.Sample && !string.IsNullOrEmpty(x.SampleId)).ToArray();
        var requestUidItems = sampleResults.Select(x => new RequestUid { SampleId = x.SampleId, RequestCreatedDate = x.RequestCreatedDate })
            .DistinctBy(x => (x.SampleId, x.RequestCreatedDate)).ToList();

        var sampleToRequestMap = await sender.Send(new GetRequestIdForSampleIdsQuery(requestUidItems), cancellationToken);

        foreach (var result in sampleResults)
        {
            if (sampleToRequestMap.TryGetValue((result.SampleId, result.RequestCreatedDate), out var requestId))
            {
                result.RequestId = requestId;
            }
        }

        return results.Select(CsvMapper.MapResultDataToCsv).ToList();
    }

    /// <summary>
    /// Get the [MEAN] section of the result export from the test results in the database.
    /// </summary>
    /// <param name="analyticalRunIdList">ID of the analytical runs to be included in the export.</param>
    /// <param name="cancellationToken">Token for cancelling async operation.</param>
    /// <returns>A list of CSV serializable items.</returns>
    internal async Task<List<CsvMean>> GetMeanSection(IEnumerable<int> analyticalRunIdList, CancellationToken cancellationToken)
    {
        var query = dbContext.TestResults
            .AsNoTracking()
            .Where(result => result.AnalyticalRunId.HasValue && analyticalRunIdList.Contains(result.AnalyticalRunId.Value))
            .OrderBy(result => result.Created)
            .Select(result => new ResultExportMeanData
            {
                ResultType = result.ResultType,
                MeanResponse = result.MeanResponse,
                MeanConcentration = result.MeanConcentration,
                PercentCvResponse = result.PercentCvResponse,
                PercentCvConcentration = result.PercentCvConcentration,
                MethodName = result.MethodName,
                TestName = result.TestName,
                GroupId = result is GroupTestResult ? ((GroupTestResult)result).GroupId : null,
                SampleId = result is SampleTestResult ? ((SampleTestResult)result).RequestUid.SampleId : "",
                CalibratorName = result is CalibratorTestResult ? ((CalibratorTestResult)result).CalibratorPointName : "",
                CurveControlName = result is CurveControlTestResult ? ((CurveControlTestResult)result).CurveControlPointName : "",
                QualityControlName = result is QualityControlTestResult ? ((QualityControlTestResult)result).QualityControlName : ""
            });

        var results = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return results.Select(CsvMapper.MapMeanDataToCsv).ToList();
    }

    private static ResultExportType GetExportType(string? cmrRunId, bool isSingleRun)
    {
        if (!string.IsNullOrWhiteSpace(cmrRunId))
        {
            return ResultExportType.ByCustomRunIdentity;
        }

        return isSingleRun ? ResultExportType.BySingleAnalyticalRun : ResultExportType.ByMultipleAnalyticalRuns;
    }

    private async Task<string> GetExportedByUserName(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return string.Empty;
        }
        var user = await sender.Send(new GetApplicationUserByIdQuery(userId.Value), cancellationToken);
        return user?.FullName ?? user?.UserName ?? string.Empty;
    }
}
