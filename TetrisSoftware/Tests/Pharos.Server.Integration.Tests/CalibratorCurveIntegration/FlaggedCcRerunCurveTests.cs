using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class FlaggedCcRerunCurveTests(IntegrationTestsWaf factory) : CalibratorCurveIntegrationTestBase(factory)
{
    private const string _curveControlPointName = "CC-1";
    private const string _curveControlTestName = "a-IgE";
    private const string _sampleTestName = "c1";
    private const string _calibratorTestName = "a_IgE";
    private const string _sampleId = "SID";

    [Fact]
    public async Task RerunCurve_Should_SetWaitingState_And_PreserveOldCurveId_WithoutRecalculation()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        SetupData setup;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            setup = await ArrangeFlaggedCcRunAsync(session, instrument.SerialNumber);
            await SendRerunCalibratorCurveAsync(session.Mediator, setup.RunId);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == setup.RunId, TestContext.Current.CancellationToken);
        var sample = await assertDb.SampleTestResults
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == setup.RunId, TestContext.Current.CancellationToken);

        // New logic: the old calculation curve id is preserved for reference while waiting for a new one.
        run.CurveState.Should().Be(CurveState.WaitingForNewCurve);
        run.CurveId.Should().Be(setup.InitialCurveId);
        run.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.NotJudged);

        // WaitingForNewCurve blocks the concentration pipeline — sample values must remain reset.
        sample.Status.Should().Be(TestResultStatus.PendingCalibration);
        sample.MeanConcentration.Should().BeNull();
        sample.MeanConcentrationStatus.Should().Be(ConcentrationStatus.NotAvailable);
        sample.Replicates!.First().Concentration.Should().BeNull();
    }

    [Fact]
    public async Task RerunCurve_AltI_WhenNewCurveIsOk_Should_AssignFittedCurve_And_RecalculateSampleConcentration()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        SetupData setup;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            setup = await ArrangeFlaggedCcRunAsync(session, instrument.SerialNumber);

            await SendRerunCalibratorCurveAsync(session.Mediator, setup.RunId);

            await SendBlankAsync(session.Mediator, instrument.SerialNumber, meanValue: 0);
            await SendCalibratorGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: setup.InstrumentAnalyticalRunId,
                assayRunId: setup.AssayRunId,
                testName: _calibratorTestName,
                calibrators: BuildSigeCalibratorPoints(),
                methodName: DefaultMethodName,
                calibratorBarcode: DefaultCalibratorCartridgeBarcode,
                cancellationToken: TestContext.Current.CancellationToken);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == setup.RunId, TestContext.Current.CancellationToken);
        var curve = await assertDb.Curves.FirstAsync(c => c.Id == run.CurveId, TestContext.Current.CancellationToken);
        var sample = await assertDb.SampleTestResults
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == setup.RunId, TestContext.Current.CancellationToken);

        run.CurveSource.Should().Be(CurveSource.CurveFromCurrentAssay);
        run.CurveState.Should().Be(CurveState.Frozen);
        run.CurveId.Should().NotBeNull();
        run.CurveId.Should().NotBe(setup.InitialCurveId);
        curve.CurveControlDisabledFlag.Should().BeFalse();

        curve.Judgment.Should().Be(CurveJudgment.OK);
        curve.CurveFittedFlag.Should().BeTrue();
        curve.CalculationParameterA.Should().NotBeNull();
        curve.CalculationParameterB.Should().NotBeNull();
        curve.CalculationParameterC.Should().NotBeNull();
        curve.CalculationParameterD.Should().NotBeNull();

        sample.MeanConcentration.Should().NotBeNull();
        sample.MeanConcentrationStatus.Should().NotBe(ConcentrationStatus.NotAvailable);
        sample.Replicates!.First().Concentration.Should().NotBeNull();
    }

    [Fact]
    public async Task RerunCurve_AltII_WhenAssayEndsWithoutNewCurve_Should_SetInvalidCurveState_AndSetRunReady()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        SetupData setup;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            setup = await ArrangeFlaggedCcRunAsync(session, instrument.SerialNumber);

            await SendRerunCalibratorCurveAsync(session.Mediator, setup.RunId);

            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                setup.InstrumentAnalyticalRunId,
                setup.AssayRunId);
            await SendAssayEndAsync(session.Mediator, instrument.SerialNumber, setup.AssayRunId);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == setup.RunId, TestContext.Current.CancellationToken);

        run.RunStatus.Should().Be(RunStatus.Ready);
        run.CurveState.Should().Be(CurveState.Invalid);
        run.CurveSource.Should().Be(CurveSource.Missing);
        run.CurveId.Should().BeNull();
    }

    [Fact]
    public async Task RerunCurve_AltII_WhenAssayEndsWithPreviousCurveSource_Should_ClearCurveAssignment_ForManualSelection()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        SetupData setup;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            setup = await ArrangeFlaggedCcRunAsync(session, instrument.SerialNumber);

            var run = await session.ResultDbContext.AnalyticalRuns.FirstAsync(a => a.Id == setup.RunId, TestContext.Current.CancellationToken);
            run.CurveSource = CurveSource.CurveFromPreviousAssay;
            await session.ResultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            await SendRerunCalibratorCurveAsync(session.Mediator, setup.RunId);

            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                setup.InstrumentAnalyticalRunId,
                setup.AssayRunId);
            await SendAssayEndAsync(session.Mediator, instrument.SerialNumber, setup.AssayRunId);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var runAfter = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == setup.RunId, TestContext.Current.CancellationToken);

        runAfter.RunStatus.Should().Be(RunStatus.Ready);
        runAfter.CurveState.Should().Be(CurveState.Invalid);
        runAfter.CurveSource.Should().Be(CurveSource.Missing);
        runAfter.CurveId.Should().BeNull();
    }

    [Fact]
    public async Task RerunCurve_Should_RemoveResultIssueTaskCard_WhenCurveWasFlagged()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var taskCardService = ServiceProvider.GetRequiredService<ITaskCardService>();
        SetupData setup;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            setup = await ArrangeFlaggedCcRunAsync(session, instrument.SerialNumber);

            // Pre-condition: seed the task card to reflect the flagged state.
            await session.Mediator.Send(
                new TaskCardCommand(TaskCardType.CalibrationIssue, TaskCardState.Warning, instrument.SerialNumber),
                TestContext.Current.CancellationToken);

            taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
                .Should().BeTrue("a Warning task card must exist while the CC is flagged");

            await SendRerunCalibratorCurveAsync(session.Mediator, setup.RunId);
        } // Dispose session so DelayedPublisher flushes notifications into the background channel.

        await WaitForNotificationsAsync();

        taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
            .Should().BeFalse("the CalibrationIssue task card must be removed when a re-run curve is ordered after a flagged curve");
    }

    private static async Task<SetupData> ArrangeFlaggedCcRunAsync(CalibrationTestSession session, string serialNumber)
    {
        await SeedMethodForCurveAndCcAsync(session.ParametersDbContext);

        var data = await CreateTestData(serialNumber)
            .WithAnalyticalRun(issue: AnalyticalRunIssue.Warning)
            .WithCurve(CurveJudgment.OK)
            .WithCurveControlResult(_curveControlPointName, testName: _curveControlTestName, analyticalRunIndex: 0)
            .BuildAsync(session.ResultDbContext);

        var run = data.AnalyticalRuns[0];
        var initialCurve = data.Curves[0];

        initialCurve.InstrumentRunId = Guid.NewGuid();
        run.CurveId = initialCurve.Id;
        run.CurveSource = CurveSource.CurveFromPreviousAssay;
        run.CurveControlTotalJudgment = CurveControlJudgmentResult.Flagged;
        run.CurveState = CurveState.Invalid;
        run.CurveControlIssue = AnalyticalRunIssue.Warning;

        await session.ResultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SendBlankAsync(session.Mediator, serialNumber, meanValue: 0);
        await SendSampleGroupAsync(
            session.Mediator,
            serialNumber,
            analyticalRunId: run.InstrumentAnalyticalRunId.ToString(),
            assayRunId: run.InstrumentRunId.ToString(),
            methodName: DefaultMethodName,
            testName: _sampleTestName,
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
        return new SetupData(run.Id, run.InstrumentAnalyticalRunId.ToString(), run.InstrumentRunId.ToString(), initialCurve.Id);
    }

    private sealed record SetupData(int RunId, string InstrumentAnalyticalRunId, string AssayRunId, int InitialCurveId);
}
