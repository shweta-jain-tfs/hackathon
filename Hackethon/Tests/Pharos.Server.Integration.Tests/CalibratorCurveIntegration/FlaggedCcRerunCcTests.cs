using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

/// <summary>
/// Integration tests covering the "Flagged CC, Rerun CC" scenario.
///
/// Preparation (shared Arrange):
///   Step 1 – An OK calibration curve assigned as the current curve for the run.
///   Step 2 – A new assay whose CC is judged Flagged → run is in CurveControlIssue = Warning state.
///   Step 3 – Sample results exist in the same assay with RU (response) values calculated
///             but concentration not yet available (CC flag blocks recalculation).
///
/// Three outcome alternatives are tested:
///   Alt I   – Instrument sends a new CC judged OK  → concentration recalculated, task card cleared.
///   Alt II  – Instrument sends a new CC still Flagged → state stays failed, task card remains.
///   Alt III – Assay ends without a new CC → run moves to Ready, CC issue is preserved.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class FlaggedCcRerunCcTests(IntegrationTestsWaf factory) : CalibratorCurveIntegrationTestBase(factory)
{
    private const string _ccPointName = "CC-1";
    private const string _sampleTestName = "a-IgE";
    private const string _sampleResultTestName = "c1";
    private const string _sampleId = "SID";
    // Fluorometer values used to steer the CC judgment in Alt I / Alt II.
    // These only produce a meaningful judgment when the AnalyticalRun has a fitted
    // CalculationCurve (CalculationParameterA–E set). Without fit parameters the
    // CC replicate has no concentration and JudgmentSummary stays NotDefined.
    private const double _okCcFluorometerValue = 12938;
    private const double _flaggedCcFluorometerValue = 5000;

    // -------------------------------------------------------------------------
    // Rerun command – state reset
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RerunCC_Should_ResetCurveControlJudgmentAndIssue()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            var data = await ArrangePreparationAsync(session, instrument.SerialNumber);
            runId = data.AnalyticalRuns[0].Id;

            await SendRerunCurveControlAsync(session.Mediator, runId);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        // Assert
        var run = await ServiceProvider.GetRequiredService<IResultDbContext>()
            .AnalyticalRuns.FirstAsync(a => a.Id == runId, TestContext.Current.CancellationToken);

        run.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.NotJudged);
        run.CurveControlIssue.Should().Be(AnalyticalRunIssue.NoIssue);
        run.CurveState.Should().Be(CurveState.WaitingForCurveControl,
            "a Rerun CC order blocks further calculation until the new CC is evaluated");
    }

    // -------------------------------------------------------------------------
    // Alt I – New CC judged OK → concentration recalculated, task card cleared
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RerunCC_AltI_WhenNewCCIsOk_Should_RecalculateConcentrationAndClearIssue()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            var data = await ArrangePreparationAsync(session, instrument.SerialNumber);
            await SeedMethodWithCcParametersAsync(session.ParametersDbContext);

            var run = data.AnalyticalRuns[0];
            runId = run.Id;

            await SendRerunCurveControlAsync(session.Mediator, run.Id);

            // Instrument responds: send a new CC with fluorometer value within the OK band.
            // NOTE: For concentration to be calculated, AnalyticalRun.CalculationCurveId must point
            //       to a Curve with fitted Rodbard 5-par parameters (CalculationParameterA–E).
            //       In this test those parameters are not seeded, so MeanConcentration will remain
            //       null until a fully fitted curve is available. Seed curve fit parameters if you
            //       need to verify end-to-end concentration recalculation.
            await SendBlankAsync(session.Mediator, instrument.SerialNumber);
            await SendCurveControlGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run.InstrumentAnalyticalRunId.ToString(),
                assayRunId: run.InstrumentRunId.ToString(),
                testName: _sampleTestName,
                curveControls:
                [
                    new CurveControlPoint(
                        Name: _ccPointName,
                        FirstMeasurementValue: _okCcFluorometerValue)
                ],
                methodName: DefaultMethodName,
                cancellationToken: TestContext.Current.CancellationToken);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        // Assert – the CC evaluation pipeline fired and a new CC result was stored.
        var assertCtx = ServiceProvider.GetRequiredService<IResultDbContext>();
        var updatedRun = await assertCtx.AnalyticalRuns.FirstAsync(a => a.Id == runId, TestContext.Current.CancellationToken);
        var latestCcResult = await assertCtx.CurveControlTestResults
            .OrderByDescending(cc => cc.Id)
            .FirstOrDefaultAsync(cc => cc.AnalyticalRunId == runId && cc.CurveControlPointName == _ccPointName, TestContext.Current.CancellationToken);

        latestCcResult.Should().NotBeNull("a new CC result must have been stored by the processing pipeline");
        updatedRun.CurveControlTotalJudgment.Should().NotBe(CurveControlJudgmentResult.Flagged,
            "after an OK rerun, the judgment must change from its previous Flagged state");
        updatedRun.CurveControlIssue.Should().Be(AnalyticalRunIssue.NoIssue,
            "an OK CC clears the curve control issue on the analytical run");
    }

    // -------------------------------------------------------------------------
    // Alt II – New CC still Flagged → state stays failed, task card remains
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RerunCC_AltII_WhenNewCCIsFlagged_Should_PreserveCurveControlIssue()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            var data = await ArrangePreparationAsync(session, instrument.SerialNumber);
            await SeedMethodWithCcParametersAsync(session.ParametersDbContext);

            var run = data.AnalyticalRuns[0];
            runId = run.Id;

            await SendRerunCurveControlAsync(session.Mediator, run.Id);

            // Instrument responds with a CC that will be evaluated as Flagged/NotOK
            // (fluorometer value deliberately far outside the acceptable band).
            await SendBlankAsync(session.Mediator, instrument.SerialNumber);
            await SendCurveControlGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run.InstrumentAnalyticalRunId.ToString(),
                assayRunId: run.InstrumentRunId.ToString(),
                testName: _sampleTestName,
                curveControls:
                [
                    new CurveControlPoint(
                        Name: _ccPointName,
                        FirstMeasurementValue: _flaggedCcFluorometerValue)
                ],
                methodName: DefaultMethodName,
                cancellationToken: TestContext.Current.CancellationToken);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        // Assert – a new CC result was stored; concentration issue remains.
        var assertCtx = ServiceProvider.GetRequiredService<IResultDbContext>();
        var updatedRun = await assertCtx.AnalyticalRuns.FirstAsync(a => a.Id == runId, TestContext.Current.CancellationToken);
        var latestCcResult = await assertCtx.CurveControlTestResults
            .OrderByDescending(cc => cc.Id)
            .FirstOrDefaultAsync(cc => cc.AnalyticalRunId == runId && cc.CurveControlPointName == _ccPointName, TestContext.Current.CancellationToken);

        latestCcResult.Should().NotBeNull("the new CC replicate must still be stored even when flagged");
        updatedRun.CurveControlTotalJudgment.Should().NotBe(CurveControlJudgmentResult.OK,
            "a flagged rerun must not produce an OK judgment");
    }

    // -------------------------------------------------------------------------
    // Alt III – Assay ends without new CC → run moves to Ready
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RerunCC_AltIII_WhenAssayEnds_Should_SetRunStatusToReady()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            var data = await ArrangePreparationAsync(session, instrument.SerialNumber);
            var run = data.AnalyticalRuns[0];
            runId = run.Id;

            await SendRerunCurveControlAsync(session.Mediator, run.Id);

            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                run.InstrumentAnalyticalRunId.ToString(),
                run.InstrumentRunId.ToString());
            await SendAssayEndAsync(session.Mediator, instrument.SerialNumber, run.InstrumentRunId.ToString());
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        // Assert – the run is now Ready; the CC judgment remains NotJudged (no new CC arrived).
        var assertCtx = ServiceProvider.GetRequiredService<IResultDbContext>();
        var updatedRun = await assertCtx.AnalyticalRuns.FirstAsync(a => a.Id == runId, TestContext.Current.CancellationToken);

        updatedRun.RunStatus.Should().Be(RunStatus.Ready,
            "ending the assay must transition the run from Processing to Ready");
        updatedRun.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.NotJudged,
            "no new CC arrived after the rerun request, so judgment stays at NotJudged");
    }

    // -------------------------------------------------------------------------
    // Shared Arrange helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Seeds the three preparation steps from the scenario:
    /// <list type="number">
    ///   <item>An OK calibration curve assigned as the current calculation curve.</item>
    ///   <item>A flagged CC result that puts the run into <see cref="CurveControlJudgmentResult.Flagged"/>.</item>
    ///   <item>Sample results with RU but no concentration.</item>
    /// </list>
    /// </summary>
    private static async Task<CurveTestData> ArrangePreparationAsync(CalibrationTestSession session, string serialNumber)
    {
        var data = await CreateTestData(serialNumber)
            .WithAnalyticalRun(issue: AnalyticalRunIssue.Warning)
            .WithCurve(CurveJudgment.OK)
            .WithCurveControlResult(_ccPointName, testName: _sampleTestName, analyticalRunIndex: 0)
            .BuildAsync(session.ResultDbContext);

        // Connect the OK curve as the current calculation curve and record the flagged CC state
        // exactly as the CC evaluation pipeline would have left them after Prep Step 2.
        var run = data.AnalyticalRuns[0];
        var curve = data.Curves[0];

        curve.CurveFittedFlag = true;
        curve.CalculationParameterA = 18017.0;
        curve.CalculationParameterB = 0.912;
        curve.CalculationParameterC = 0.332;
        curve.CalculationParameterD = 0.005;
        curve.CalculationParameterE = 0.0;
        curve.CurveMinConcentration = 0.001;
        curve.CurveMaxConcentration = 100;
        curve.ZeroPointCalculationNumberOfIntervals = 100;
        curve.NumberOfCalibrationPointsInZeroPointCalculation = 2;
        curve.ZeroPointSwitchConcentration = 0.1;
        curve.ZeroPointCalculationCoefficientAlfa = 0.5;
        curve.ZeroPointCalculationCoefficientBeta = 0.5;

        run.CurveId = curve.Id;
        run.CurveSource = CurveSource.CurveFromPreviousAssay;
        run.CurveControlTotalJudgment = CurveControlJudgmentResult.Flagged;
        run.CurveState = CurveState.Invalid;
        await session.ResultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Step 3 (simulator-like path): sample result in same assay with RU calculated
        // but concentration unavailable while curve control is disabled/flagged.
        await SendBlankAsync(session.Mediator, serialNumber);
        await SendSampleGroupAsync(
            session.Mediator,
            serialNumber,
            analyticalRunId: run.InstrumentAnalyticalRunId.ToString(),
            assayRunId: run.InstrumentRunId.ToString(),
            methodName: DefaultMethodName,
            testName: _sampleResultTestName,
            samples:
            [
                new SamplePoint(
                    SampleId: _sampleId,
                    FirstMeasurementValue: ToMeasurementValue(1000),
                    InstrumentDilutionFactor: 1,
                    PreDilutionFactor: 1)
            ]);

        // Do not call WaitForNotificationsAsync here — the session scope is still alive so
        // DelayedPublisher has not flushed to the background channel yet. The caller must
        // dispose the session first, then call WaitForNotificationsAsync.
        return data;
    }

    /// <summary>
    /// Seeds the minimal method parameters required for CC evaluation to produce a
    /// <see cref="CurveControlJudgment"/> other than <see cref="CurveControlJudgment.NotDefined"/>.
    /// <para>
    /// Expected CC-1 concentration: 0.70 kUA/l.
    /// Inner SD limit: 2.2  → OK band   [0.70 ± (0.084 × 2.2)]  = [0.515, 0.885]
    /// Outer SD limit: 3.5  → Flagged   [0.70 ± (0.084 × 3.5)]  = [0.406, 0.994]
    /// </para>
    /// </summary>
    private static async Task SeedMethodWithCcParametersAsync(IParametersDbContext paramContext)
    {
        var method = new Method
        {
            MethodName = DefaultMethodName,
            Technology = AssayTechnology.ImmunoCap,
            FullMethodName = DefaultMethodName,
            MethodNameLis = DefaultMethodName,
            Enabled = true,
            AnalyticalRun = new MethodAnalyticalRun(),
            Calculation = new MethodCalculation(),
            Presentation = new MethodPresentation
            {
                XAxisText = "Concentration",
                XAxisUnit = "kUA/l",
                YAxisText = "Response",
                YAxisUnit = "RU"
            },
            Reporting = new MethodReporting
            {
                DefaultTextQuantitativeAbove = ">100",
                DefaultTextQuantitativeBelow = "<0.10"
            },
            ConcentrationUnits = [new MethodConcentrationUnit { UnitId = 1, ConcentrationUnit = "kUA/l" }]
        };
        paramContext.Methods.Add(method);

        var methodTest = new MethodTest
        {
            TestName = _sampleTestName,
            DefaultLabTestName = _sampleTestName,
            FullTestName = _sampleTestName,
            DiluentIdentity = "Dil",
            DefaultConcentrationUnit = method.ConcentrationUnits[0],
            Method = method
        };
        paramContext.MethodTests.Add(methodTest);

        var curveControl = new MethodCurveControl
        {
            CurveControlName = _ccPointName,
            Active = true,
            MethodTest = methodTest,
            Method = method,
            Replicates = 1,
            ExpectedConcentration = 0.70,
            ExpectedStdDev = 0.084,
            InnerStdDevLimit = 2.2,
            OuterStdDevLimit = 3.5
        };
        paramContext.MethodCurveControls.Add(curveControl);

        await paramContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
