using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class CalibrationStateAcrossRunsTests(IntegrationTestsWaf factory) : CalibratorCurveIntegrationTestBase(factory)
{
    private const string _curveControlPointName = "CC-1";
    private const string _curveControlTestName = "a-IgE";
    private const string _sampleTestName = "c1";
    private const string _calibratorTestName = "a_IgE";
    private const string _sampleId = "SID";
    private const double _okCcFluorometerValue = 12.938;   // Simulator signal for response 320
    private const double _flaggedCcFluorometerValue = 8.138; // Simulator signal for response 200 (flagged)

    [Fact]
    public async Task ThreeRuns_CalibrationOkFailOk_Should_KeepPerRunStates_AndUpdateDefaultCurve()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();

        await using (var setupSession = CreateSession())
        {
            SetInstrumentInAssayMode(setupSession.Scope, instrument.SerialNumber);
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        // Run 1: Calibration OK
        var run1InstrumentAnalyticalRunId = Guid.NewGuid();
        var run1AssayRunId = Guid.NewGuid();
        await using (var session = CreateSession())
        {
            await SendBlankAsync(session.Mediator, instrument.SerialNumber, meanValue: 0);
            await SendSampleGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run1InstrumentAnalyticalRunId.ToString(),
                assayRunId: run1AssayRunId.ToString(),
                methodName: DefaultMethodName,
                testName: _sampleTestName,
                samples: [new SamplePoint(_sampleId, ToMeasurementValue(1000))],
                cancellationToken: TestContext.Current.CancellationToken);
            await SendCalibratorGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run1InstrumentAnalyticalRunId.ToString(),
                assayRunId: run1AssayRunId.ToString(),
                testName: _calibratorTestName,
                calibrators: BuildSigeCalibratorPoints(),
                methodName: DefaultMethodName,
                calibratorBarcode: DefaultCalibratorCartridgeBarcode,
                cancellationToken: TestContext.Current.CancellationToken);
            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                run1InstrumentAnalyticalRunId.ToString(),
                run1AssayRunId.ToString());
        } // Dispose session so DelayedPublisher flushes notifications

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run1 = await assertDb.AnalyticalRuns.FirstAsync(a =>
            a.InstrumentNumber == instrument.SerialNumber
            && a.InstrumentAnalyticalRunId == run1InstrumentAnalyticalRunId
            && a.InstrumentRunId == run1AssayRunId, TestContext.Current.CancellationToken);
        var run1After = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == run1.Id, TestContext.Current.CancellationToken);
        var run1Sample = await assertDb.SampleTestResults.Include(s => s.Replicates).FirstAsync(s => s.AnalyticalRunId == run1.Id, TestContext.Current.CancellationToken);
        var defaultCurveAfterRun1 = await assertDb.CurrentCurves
            .Include(c => c.Curve)
            .FirstOrDefaultAsync(c => c.MethodName == DefaultMethodName && c.ConjugateLotNumber == DefaultConjugateLot && c.Curve.InstrumentNumber == instrument.SerialNumber, TestContext.Current.CancellationToken);

        run1After.CurveId.Should().NotBeNull();
        run1After.CalibrationCurveIssue.Should().Be(AnalyticalRunIssue.NoIssue);
        run1Sample.MeanConcentration.Should().NotBeNull();
        run1Sample.Replicates!.First().Response.Should().NotBeNull();
        defaultCurveAfterRun1.Should().NotBeNull();
        defaultCurveAfterRun1!.CurveId.Should().Be(run1After.CurveId);

        // Run 2: New analytical run in a different assay should pick curve from previous assay
        var run2InstrumentAnalyticalRunId = Guid.NewGuid();
        var run2AssayRunId = Guid.NewGuid();

        await using (var session = CreateSession())
        {
            await SendBlankAsync(session.Mediator, instrument.SerialNumber, meanValue: 0);
            await SendSampleGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run2InstrumentAnalyticalRunId.ToString(),
                assayRunId: run2AssayRunId.ToString(),
                methodName: DefaultMethodName,
                testName: _sampleTestName,
                samples: [new SamplePoint($"{_sampleId}-2", ToMeasurementValue(1000))],
                cancellationToken: TestContext.Current.CancellationToken);
        } // Dispose session so DelayedPublisher flushes notifications

        await WaitForNotificationsAsync();

        var run2 = await assertDb.AnalyticalRuns.FirstAsync(a =>
            a.InstrumentNumber == instrument.SerialNumber
            && a.InstrumentAnalyticalRunId == run2InstrumentAnalyticalRunId
            && a.InstrumentRunId == run2AssayRunId, TestContext.Current.CancellationToken);

        var run2BeforeCc = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == run2.Id, TestContext.Current.CancellationToken);
        var run2SampleBeforeCc = await assertDb.SampleTestResults.Include(s => s.Replicates).FirstAsync(s => s.AnalyticalRunId == run2.Id, TestContext.Current.CancellationToken);

        run2BeforeCc.CurveId.Should().NotBeNull();
        run2BeforeCc.CurveSource.Should().Be(CurveSource.CurveFromPreviousAssay);
        run2BeforeCc.CurveState.Should().Be(CurveState.WaitingForCurveControl);
        run2SampleBeforeCc.MeanConcentration.Should().BeNull();
        run2SampleBeforeCc.Replicates!.First().Response.Should().NotBeNull();

        // Run 2 continued: Flagged CC should block concentrations
        await using (var session = CreateSession())
        {
            await SendCurveControlGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run2.InstrumentAnalyticalRunId.ToString(),
                assayRunId: run2.InstrumentRunId.ToString(),
                testName: _curveControlTestName,
                curveControls: [new CurveControlPoint(_curveControlPointName, FirstMeasurementValue: _flaggedCcFluorometerValue)],
                methodName: DefaultMethodName,
                cancellationToken: TestContext.Current.CancellationToken);
        } // Dispose session so DelayedPublisher flushes notifications

        await WaitForNotificationsAsync();

        var run2After = await assertDb.AnalyticalRuns.AsNoTracking().FirstAsync(a => a.Id == run2.Id, TestContext.Current.CancellationToken);
        var run2Sample = await assertDb.SampleTestResults
            .AsNoTracking()
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == run2.Id, TestContext.Current.CancellationToken);
        var run2Cc = await assertDb.CurveControlTestResults
            .AsNoTracking()
            .Where(c => c.AnalyticalRunId == run2.Id)
            .OrderByDescending(c => c.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
        var defaultCurveAfterRun2 = await assertDb.CurrentCurves
            .Include(c => c.Curve)
            .FirstOrDefaultAsync(c => c.MethodName == DefaultMethodName && c.ConjugateLotNumber == DefaultConjugateLot && c.Curve.InstrumentNumber == instrument.SerialNumber, TestContext.Current.CancellationToken);

        var run1Stable = await assertDb.AnalyticalRuns.AsNoTracking().FirstAsync(a => a.Id == run1.Id, TestContext.Current.CancellationToken);
        var run1SampleStable = await assertDb.SampleTestResults
            .AsNoTracking()
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == run1.Id, TestContext.Current.CancellationToken);
        run1Stable.CurveId.Should().NotBeNull();
        run1SampleStable.MeanConcentration.Should().NotBeNull();
        run1SampleStable.Replicates!.First().Response.Should().NotBeNull();

        run2After.CurveId.Should().NotBeNull();

        run2After.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.Flagged);
        run2Cc.JudgmentSummary.Should().Be(CurveControlJudgment.FlaggedLow);
        run2Sample.MeanConcentration.Should().BeNull();
        run2Sample.Replicates!.First().Response.Should().NotBeNull();
        defaultCurveAfterRun2.Should().BeNull();

        // Run 2 continued: Ok CC should calculate concentrations
        await using (var session = CreateSession())
        {
            await SendCurveControlGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run2.InstrumentAnalyticalRunId.ToString(),
                assayRunId: run2.InstrumentRunId.ToString(),
                testName: _curveControlTestName,
                curveControls: [new CurveControlPoint(_curveControlPointName, FirstMeasurementValue: _okCcFluorometerValue)],
                methodName: DefaultMethodName,
                cancellationToken: TestContext.Current.CancellationToken);
        } // Dispose session so DelayedPublisher flushes notifications

        await WaitForNotificationsAsync();

        var run2AfterOkCc = await assertDb.AnalyticalRuns.AsNoTracking().FirstAsync(a => a.Id == run2.Id, TestContext.Current.CancellationToken);
        var run2SampleAfterOkCc = await assertDb.SampleTestResults
            .AsNoTracking()
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == run2.Id, TestContext.Current.CancellationToken);
        var run2OkCc = await assertDb.CurveControlTestResults
            .AsNoTracking()
            .Where(c => c.AnalyticalRunId == run2.Id)
            .OrderByDescending(c => c.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
        var defaultCurveAfterRun2OkCc = await assertDb.CurrentCurves
            .Include(c => c.Curve)
            .FirstOrDefaultAsync(c => c.MethodName == DefaultMethodName && c.ConjugateLotNumber == DefaultConjugateLot && c.Curve.InstrumentNumber == instrument.SerialNumber, TestContext.Current.CancellationToken);

        run2AfterOkCc.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.OK);
        run2OkCc.JudgmentSummary.Should().Be(CurveControlJudgment.OK);
        run2SampleAfterOkCc.MeanConcentration.Should().NotBeNull();
        run2SampleAfterOkCc.Replicates!.First().Response.Should().NotBeNull();
        defaultCurveAfterRun2OkCc.Should().BeNull();
        await using (var session = CreateSession())
        {
            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                run2.InstrumentAnalyticalRunId.ToString(),
                run2.InstrumentRunId.ToString());
            await SendAssayEndAsync(session.Mediator, instrument.SerialNumber, run2.InstrumentRunId.ToString());
        } // Dispose session so DelayedPublisher flushes notifications

        await WaitForNotificationsAsync();

        // Run 3: Calibration OK again
        var run3InstrumentAnalyticalRunId = Guid.NewGuid();
        var run3AssayRunId = Guid.NewGuid();
        await using (var session = CreateSession())
        {
            await SendBlankAsync(session.Mediator, instrument.SerialNumber, meanValue: 0);
            await SendSampleGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run3InstrumentAnalyticalRunId.ToString(),
                assayRunId: run3AssayRunId.ToString(),
                methodName: DefaultMethodName,
                testName: _sampleTestName,
                samples: [new SamplePoint($"{_sampleId}-3", ToMeasurementValue(1000))],
                cancellationToken: TestContext.Current.CancellationToken);
            await SendCalibratorGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: run3InstrumentAnalyticalRunId.ToString(),
                assayRunId: run3AssayRunId.ToString(),
                testName: _calibratorTestName,
                calibrators: BuildSigeCalibratorPoints(),
                methodName: DefaultMethodName,
                calibratorBarcode: DefaultCalibratorCartridgeBarcode,
                cancellationToken: TestContext.Current.CancellationToken);
            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                run3InstrumentAnalyticalRunId.ToString(),
                run3AssayRunId.ToString());
            await SendAssayEndAsync(session.Mediator, instrument.SerialNumber, run3AssayRunId.ToString());
        } // Dispose session so DelayedPublisher flushes notifications

        await WaitForNotificationsAsync();

        var run3 = await assertDb.AnalyticalRuns.FirstAsync(a =>
            a.InstrumentNumber == instrument.SerialNumber
            && a.InstrumentAnalyticalRunId == run3InstrumentAnalyticalRunId
            && a.InstrumentRunId == run3AssayRunId, TestContext.Current.CancellationToken);
        run3.InstrumentRunId.Should().Be(run3AssayRunId);
        run3.InstrumentRunId.Should().NotBe(run2.InstrumentRunId);
        var run3After = await assertDb.AnalyticalRuns.FirstAsync(a => a.Id == run3.Id, TestContext.Current.CancellationToken);
        var run3Sample = await assertDb.SampleTestResults.Include(s => s.Replicates).FirstAsync(s => s.AnalyticalRunId == run3.Id, TestContext.Current.CancellationToken);
        var defaultCurveAfterRun3 = await assertDb.CurrentCurves
            .Include(c => c.Curve)
            .FirstOrDefaultAsync(c => c.MethodName == DefaultMethodName && c.ConjugateLotNumber == DefaultConjugateLot && c.Curve.InstrumentNumber == instrument.SerialNumber, TestContext.Current.CancellationToken);

        run1Stable = await assertDb.AnalyticalRuns.AsNoTracking().FirstAsync(a => a.Id == run1.Id, TestContext.Current.CancellationToken);
        run1SampleStable = await assertDb.SampleTestResults
            .AsNoTracking()
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == run1.Id, TestContext.Current.CancellationToken);
        run1Stable.CurveId.Should().NotBeNull();
        run1SampleStable.MeanConcentration.Should().NotBeNull();
        run1SampleStable.Replicates!.First().Response.Should().NotBeNull();

        run2After = await assertDb.AnalyticalRuns.AsNoTracking().FirstAsync(a => a.Id == run2.Id, TestContext.Current.CancellationToken);
        run2Sample = await assertDb.SampleTestResults
            .AsNoTracking()
            .Include(s => s.Replicates)
            .FirstAsync(s => s.AnalyticalRunId == run2.Id, TestContext.Current.CancellationToken);
        run2After.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.OK);
        run2Sample.MeanConcentration.Should().NotBeNull();
        run2Sample.Replicates!.First().Response.Should().NotBeNull();

        run3After.CurveId.Should().NotBeNull();
        run3Sample.MeanConcentration.Should().NotBeNull();
        run3Sample.Replicates!.First().Response.Should().NotBeNull();
        defaultCurveAfterRun3.Should().NotBeNull();
        defaultCurveAfterRun3!.CurveId.Should().Be(run3After.CurveId);
    }
}
