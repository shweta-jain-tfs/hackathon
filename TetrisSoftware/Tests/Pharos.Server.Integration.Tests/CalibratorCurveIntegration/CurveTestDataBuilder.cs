using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

/// <summary>
/// Fluent builder for seeding calibrator curve and curve control test data.
/// Use <see cref="CalibratorCurveIntegrationTestBase.CreateTestData"/> to obtain a pre-configured instance.
///
/// Example:
/// <code>
/// var data = await CreateTestData(instrumentNumber)
///     .WithAnalyticalRun(issue: AnalyticalRunIssue.Warning)
///     .WithCurve(CurveJudgment.OK)
///     .WithCurve(CurveJudgment.Flagged)
///     .WithCurveControlResult("CC-1", testName: "a-IgE", analyticalRunIndex: 0)
///     .BuildAsync(dbContext);
///
/// // data.AnalyticalRuns[0].Id is available after build.
/// </code>
/// </summary>
internal class CurveTestDataBuilder
{
    private string _instrumentNumber = "SN001";
    private string _methodName = "sIgE";
    private string _conjugateLot = "BGR01";

    private readonly List<AnalyticalRunSetup> _analyticalRuns = [];
    private readonly List<CurveSetup> _curves = [];
    private readonly List<CurveControlSetup> _curveControls = [];
    private readonly List<SampleResultSetup> _sampleResults = [];

    public CurveTestDataBuilder ForInstrument(string instrumentNumber)
    {
        _instrumentNumber = instrumentNumber;
        return this;
    }

    public CurveTestDataBuilder ForMethod(string methodName, string conjugateLot)
    {
        _methodName = methodName;
        _conjugateLot = conjugateLot;
        return this;
    }

    /// <summary>
    /// Adds an analytical run to the test data.
    /// </summary>
    /// <param name="issue">Initial curve issue flag on the run.</param>
    /// <param name="methodName">Overrides the builder's default method name for this run.</param>
    /// <param name="conjugateLot">Overrides the builder's default conjugate lot for this run.</param>
    /// <param name="curveState">Initial curve state for the run.</param>
    /// <param name="curveSource">Initial curve source for the run.</param>
    /// <param name="runStatus">Initial processing status for the run.</param>
    /// <param name="assignedCurveIndex">Optional zero-based index into curves added via <see cref="WithCurve"/> to assign to this run after persistence.</param>
    public CurveTestDataBuilder WithAnalyticalRun(
        AnalyticalRunIssue issue = AnalyticalRunIssue.NoIssue,
        string? methodName = null,
        string? conjugateLot = null,
        CurveState curveState = CurveState.Valid,
        CurveSource curveSource = CurveSource.Missing,
        RunStatus runStatus = RunStatus.Processing,
        int? assignedCurveIndex = null)
    {
        _analyticalRuns.Add(new AnalyticalRunSetup(
            MethodName: methodName ?? _methodName,
            InstrumentNumber: _instrumentNumber,
            ConjugateLot: conjugateLot ?? _conjugateLot,
            Issue: issue,
            CurveState: curveState,
            CurveSource: curveSource,
            RunStatus: runStatus,
            AssignedCurveIndex: assignedCurveIndex));
        return this;
    }

    /// <summary>
    /// Adds a calibrator curve row directly in the database (for tests that require pre-existing curves,
    /// e.g. curve selection or rerun scenarios).
    /// </summary>
    public CurveTestDataBuilder WithCurve(
        CurveJudgment judgment = CurveJudgment.OK,
        DateTimeOffset? measurementDateTime = null,
        DateOnly? lastDayToUse = null)
    {
        _curves.Add(new CurveSetup(
            InstrumentNumber: _instrumentNumber,
            MethodName: _methodName,
            ConjugateLot: _conjugateLot,
            Judgment: judgment,
            MeasurementDateTime: measurementDateTime,
            LastDayToUse: lastDayToUse));
        return this;
    }

    /// <summary>
    /// Adds a curve control test result row directly in the database.
    /// </summary>
    /// <param name="controlPointName">The curve control point name, e.g. "CC-1".</param>
    /// <param name="testName">The assay test name the control belongs to, e.g. "a-IgE".</param>
    /// <param name="analyticalRunIndex">Zero-based index into the analytical runs added via <see cref="WithAnalyticalRun"/>.</param>
    /// <param name="measurementDateTime">Measurement time; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public CurveTestDataBuilder WithCurveControlResult(
        string controlPointName,
        string testName,
        int analyticalRunIndex = 0,
        DateTimeOffset? measurementDateTime = null)
    {
        _curveControls.Add(new CurveControlSetup(
            InstrumentNumber: _instrumentNumber,
            MethodName: _methodName,
            TestName: testName,
            ControlPointName: controlPointName,
            AnalyticalRunIndex: analyticalRunIndex,
            MeasurementDateTime: measurementDateTime));
        return this;
    }

    /// <summary>
    /// Adds a sample test result with a calculated response (RU) but no concentration.
    /// This represents the state described in Prep Step 3: blank-subtracted RU is available
    /// but concentration cannot yet be calculated because the CC issue is still present.
    /// </summary>
    /// <param name="testName">Assay test name, e.g. "a-IgE".</param>
    /// <param name="analyticalRunIndex">Zero-based index into the analytical runs added via <see cref="WithAnalyticalRun"/>.</param>
    /// <param name="meanResponse">The blank-subtracted response (RU) value.</param>
    public CurveTestDataBuilder WithSampleTestResult(string testName, int analyticalRunIndex = 0, double meanResponse = 1000)
    {
        _sampleResults.Add(new SampleResultSetup(
            InstrumentNumber: _instrumentNumber,
            MethodName: _methodName,
            TestName: testName,
            AnalyticalRunIndex: analyticalRunIndex,
            MeanResponse: meanResponse));
        return this;
    }

    /// <summary>
    /// Persists all configured entities to the database in dependency order
    /// (analytical runs → curves → curve control results).
    /// Entity IDs are available on the returned <see cref="CurveTestData"/> after the call.
    /// </summary>
    public async Task<CurveTestData> BuildAsync(IResultDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var analyticalRuns = _analyticalRuns.Select(s => new AnalyticalRun
        {
            MethodName = s.MethodName,
            InstrumentNumber = s.InstrumentNumber,
            ConjugateLotNumber = s.ConjugateLot,
            CalibrationCurveIssue = s.Issue,
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid(),
            CurveState = s.CurveState,
            CurveSource = s.CurveSource,
            RunStatus = s.RunStatus
        }).ToList();

        dbContext.AnalyticalRuns.AddRange(analyticalRuns);
        await dbContext.SaveChangesAsync(cancellationToken);

        var defaultCurveRunId = analyticalRuns.Count > 0
            ? analyticalRuns[0].InstrumentRunId
            : Guid.NewGuid();

        var curves = _curves.Select(s => new Curve
        {
            InstrumentNumber = s.InstrumentNumber,
            MethodName = s.MethodName,
            ConjugateLotNumber = s.ConjugateLot,
            Judgment = s.Judgment,
            GroupId = Guid.NewGuid(),
            InstrumentRunId = defaultCurveRunId,
            MeasurementDateTime = s.MeasurementDateTime ?? DateTimeOffset.UtcNow,
            LastDayToUse = s.LastDayToUse ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        }).ToList();

        dbContext.Curves.AddRange(curves);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assign CurveId to runs that reference a curve by index
        var needsCurveAssignment = _analyticalRuns
            .Select((s, i) => (Setup: s, Run: analyticalRuns[i]))
            .Where(x => x.Setup.AssignedCurveIndex.HasValue)
            .ToList();
        foreach (var (setup, run) in needsCurveAssignment)
        {
            var idx = setup.AssignedCurveIndex!.Value;
            if (idx < curves.Count)
            {
                run.CurveId = curves[idx].Id;
            }
        }
        if (needsCurveAssignment.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var curveControlResults = _curveControls.Select(s =>
        {
            var analyticalRunId = analyticalRuns.Count > s.AnalyticalRunIndex
                ? analyticalRuns[s.AnalyticalRunIndex].Id
                : 0;

            return new CurveControlTestResult
            {
                ResultType = TestResultType.CurveControl,
                InstrumentNumber = s.InstrumentNumber,
                MethodName = s.MethodName,
                TestName = s.TestName,
                CurveControlPointName = s.ControlPointName,
                AnalyticalRunId = analyticalRunId,
                Replicates = new List<TestReplicate>
                {
                    new CurveControlTestReplicate
                    {
                        InstrumentReplicateId = Guid.NewGuid(),
                        MeasurementDateTime = s.MeasurementDateTime ?? DateTimeOffset.UtcNow
                    }
                }
            };
        }).ToList();

        dbContext.CurveControlTestResults.AddRange(curveControlResults);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sampleResults = _sampleResults.Select(s =>
        {
            var analyticalRunId = analyticalRuns.Count > s.AnalyticalRunIndex
                ? analyticalRuns[s.AnalyticalRunIndex].Id
                : 0;

            return new SampleTestResult
            {
                AnalyticalRunId = analyticalRunId,
                InstrumentNumber = s.InstrumentNumber,
                MethodName = s.MethodName,
                TestName = s.TestName,
                MeanResponse = s.MeanResponse,
                MeanConcentration = null,
                MeanConcentrationStatus = ConcentrationStatus.NotAvailable,
                Status = TestResultStatus.PendingApproval,
                ApprovalStatus = ResultApprovalStatus.NotApproved,
                TestOrderId = Guid.NewGuid(),
                Replicates = new List<TestReplicate>
                {
                    new SampleTestReplicate
                    {
                        Response = s.MeanResponse,
                        Concentration = null
                    }
                }
            };
        }).ToList();

        dbContext.SampleTestResults.AddRange(sampleResults);
        if (sampleResults.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new CurveTestData(analyticalRuns, curves, curveControlResults, sampleResults);
    }

    private record AnalyticalRunSetup(string MethodName, string InstrumentNumber, string ConjugateLot, AnalyticalRunIssue Issue, CurveState CurveState, CurveSource CurveSource, RunStatus RunStatus, int? AssignedCurveIndex);
    private record CurveSetup(string InstrumentNumber, string MethodName, string ConjugateLot, CurveJudgment Judgment, DateTimeOffset? MeasurementDateTime, DateOnly? LastDayToUse);
    private record CurveControlSetup(string InstrumentNumber, string MethodName, string TestName, string ControlPointName, int AnalyticalRunIndex, DateTimeOffset? MeasurementDateTime);
    private record SampleResultSetup(string InstrumentNumber, string MethodName, string TestName, int AnalyticalRunIndex, double MeanResponse);
}

/// <summary>
/// Holds the persisted entities produced by <see cref="CurveTestDataBuilder.BuildAsync"/>.
/// All entity IDs are assigned and available for use in assertions and commands.
/// </summary>
internal record CurveTestData(
    IReadOnlyList<AnalyticalRun> AnalyticalRuns,
    IReadOnlyList<Curve> Curves,
    IReadOnlyList<CurveControlTestResult> CurveControlResults,
    IReadOnlyList<SampleTestResult> SampleResults);
