using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

/// <summary>
/// Verifies that EndDateTime, RunStatus, CurveState, and CurveSource are aligned consistently
/// across EndAnalyticalRunCommandHandler, EndAssayRunCommandHandler, and
/// StartAnalyticalRunCommandHandler (cleanup of previously-processing runs).
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class AnalyticalRunCleanupTests(IntegrationTestsWaf factory) : CalibratorCurveIntegrationTestBase(factory)
{
    // -------------------------------------------------------------------------
    // EndAnalyticalRunCommandHandler
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(CurveState.WaitingForNewCurve, CurveSource.Missing)]
    [InlineData(CurveState.WaitingForCurveControl, CurveSource.CurveFromPreviousAssay)]
    public async Task EndAnalyticalRun_WaitingRun_SetsAllCleanupFields(CurveState initialCurveState, CurveSource initialCurveSource)
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        var data = await CreateTestData(instrument.SerialNumber)
            .WithCurve(CurveJudgment.OK)
            .WithAnalyticalRun(
                curveState: initialCurveState,
                curveSource: initialCurveSource,
                runStatus: RunStatus.Processing,
                assignedCurveIndex: initialCurveSource == CurveSource.CurveFromPreviousAssay ? 0 : null)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var seededRun = data.AnalyticalRuns[0];

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                seededRun.InstrumentAnalyticalRunId.ToString(),
                seededRun.InstrumentRunId.ToString());
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);

        run.EndDateTime.Should().NotBeNull("run should be stamped with an end time");
        run.RunStatus.Should().Be(RunStatus.ReadyWithoutTests, "no sample tests were added");
        run.CurveState.Should().Be(CurveState.Invalid, "waiting state must be invalidated on run end");

        if (initialCurveSource == CurveSource.CurveFromPreviousAssay)
        {
            run.CurveId.Should().BeNull("CurveId must be cleared when source was CurveFromPreviousAssay");
            run.CurveSource.Should().Be(CurveSource.Missing, "CurveSource must be reset to Missing");
        }
    }

    [Fact]
    public async Task EndAnalyticalRun_WithSampleTests_SetsRunStatusReady()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        var data = await CreateTestData(instrument.SerialNumber)
            .WithAnalyticalRun(
                curveState: CurveState.WaitingForNewCurve,
                curveSource: CurveSource.Missing,
                runStatus: RunStatus.Processing)
            .WithSampleTestResult("a-IgE", analyticalRunIndex: 0)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var seededRun = data.AnalyticalRuns[0];

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            await SendAnalyticalRunEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                DefaultMethodName,
                seededRun.InstrumentAnalyticalRunId.ToString(),
                seededRun.InstrumentRunId.ToString());
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);

        run.EndDateTime.Should().NotBeNull();
        run.RunStatus.Should().Be(RunStatus.Ready, "run has sample tests so status must be Ready");
        run.CurveState.Should().Be(CurveState.Invalid);
    }

    // -------------------------------------------------------------------------
    // EndAssayRunCommandHandler
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(CurveState.WaitingForNewCurve, CurveSource.Missing)]
    [InlineData(CurveState.WaitingForCurveControl, CurveSource.CurveFromPreviousAssay)]
    public async Task EndAssayRun_WaitingRun_SetsAllCleanupFields(CurveState initialCurveState, CurveSource initialCurveSource)
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        var data = await CreateTestData(instrument.SerialNumber)
            .WithCurve(CurveJudgment.OK)
            .WithAnalyticalRun(
                curveState: initialCurveState,
                curveSource: initialCurveSource,
                runStatus: RunStatus.Processing,
                assignedCurveIndex: initialCurveSource == CurveSource.CurveFromPreviousAssay ? 0 : null)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var seededRun = data.AnalyticalRuns[0];

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            await SendAssayEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                seededRun.InstrumentRunId.ToString());
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);

        run.EndDateTime.Should().NotBeNull("run should be stamped with an end time on assay end");
        run.RunStatus.Should().Be(RunStatus.ReadyWithoutTests, "no sample tests were added");
        run.CurveState.Should().Be(CurveState.Invalid, "waiting state must be invalidated on assay end");

        if (initialCurveSource == CurveSource.CurveFromPreviousAssay)
        {
            run.CurveId.Should().BeNull("CurveId must be cleared when source was CurveFromPreviousAssay");
            run.CurveSource.Should().Be(CurveSource.Missing, "CurveSource must be reset to Missing");
        }
    }

    [Fact]
    public async Task EndAssayRun_FrozenRun_OnlyRunStateIsReevaluated()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        var data = await CreateTestData(instrument.SerialNumber)
            .WithAnalyticalRun(
                curveState: CurveState.Frozen,
                curveSource: CurveSource.CurveFromCurrentAssay,
                runStatus: RunStatus.Ready)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var seededRun = data.AnalyticalRuns[0];

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            await SendAssayEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                seededRun.InstrumentRunId.ToString());
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);

        run.CurveState.Should().Be(CurveState.Frozen, "frozen runs must keep curve state on assay end");
        run.RunStatus.Should().Be(RunStatus.ReadyWithoutTests, "assay-end cleanup should re-evaluate run status based on test presence");
    }

    [Fact]
    public async Task EndAssayRun_AlreadyEndedReadyRun_IsIgnored()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        var data = await CreateTestData(instrument.SerialNumber)
            .WithAnalyticalRun(
                curveState: CurveState.Frozen,
                curveSource: CurveSource.CurveFromCurrentAssay,
                runStatus: RunStatus.Ready)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var seededRun = data.AnalyticalRuns[0];

        var setupDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var runToUpdate = await setupDb.AnalyticalRuns
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);
        var originalEndDateTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        runToUpdate.EndDateTime = originalEndDateTime;
        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            await SendAssayEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                seededRun.InstrumentRunId.ToString());
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);

        run.EndDateTime.Should().Be(originalEndDateTime, "already-ended ready runs should be filtered out on assay end");
        run.RunStatus.Should().Be(RunStatus.Ready, "already-ended ready runs should not be reprocessed");
        run.CurveState.Should().Be(CurveState.Frozen, "already-ended ready runs should remain untouched");
    }

    [Fact]
    public async Task EndAssayRun_AlreadyEndedProcessingRun_IsReprocessed()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        var data = await CreateTestData(instrument.SerialNumber)
            .WithAnalyticalRun(
                curveState: CurveState.WaitingForNewCurve,
                curveSource: CurveSource.Missing,
                runStatus: RunStatus.Processing)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var seededRun = data.AnalyticalRuns[0];

        var setupDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var runToUpdate = await setupDb.AnalyticalRuns
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);
        var originalEndDateTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        runToUpdate.EndDateTime = originalEndDateTime;
        await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);
            await SendAssayEndAsync(
                session.Mediator,
                instrument.SerialNumber,
                seededRun.InstrumentRunId.ToString());
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == seededRun.Id, TestContext.Current.CancellationToken);

        run.EndDateTime.Should().Be(originalEndDateTime, "cleanup should not overwrite an already-set end time");
        run.RunStatus.Should().Be(RunStatus.ReadyWithoutTests, "processing runs are re-evaluated to ready state even when already ended");
        run.CurveState.Should().Be(CurveState.Invalid, "waiting curve state is invalidated during cleanup");
    }

    // -------------------------------------------------------------------------
    // StartAnalyticalRunCommandHandler — cleanup of previously-processing runs
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(CurveState.WaitingForNewCurve, CurveSource.Missing)]
    [InlineData(CurveState.WaitingForCurveControl, CurveSource.CurveFromPreviousAssay)]
    public async Task StartAnalyticalRun_PreviousProcessingRun_SetsAllCleanupFields(CurveState initialCurveState, CurveSource initialCurveSource)
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();

        await using (var setupSession = CreateSession())
        {
            SetInstrumentInAssayMode(setupSession.Scope, instrument.SerialNumber);
            await SeedMethodForCurveAndCcAsync(setupSession.ParametersDbContext);
        }

        // Seed a pre-existing processing run that should be cleaned up when the next run starts
        var data = await CreateTestData(instrument.SerialNumber)
            .WithCurve(CurveJudgment.OK)
            .WithAnalyticalRun(
                curveState: initialCurveState,
                curveSource: initialCurveSource,
                runStatus: RunStatus.Processing,
                assignedCurveIndex: initialCurveSource == CurveSource.CurveFromPreviousAssay ? 0 : null)
            .BuildAsync(dbContext, TestContext.Current.CancellationToken);

        var stalledRun = data.AnalyticalRuns[0];

        // Start a new analytical run for the same instrument + method — this triggers cleanup of the stalled run
        var newAnalyticalRunId = Guid.NewGuid();
        var newAssayRunId = Guid.NewGuid();

        await using (var session = CreateSession())
        {
            await SendBlankAsync(session.Mediator, instrument.SerialNumber, meanValue: 0);
            await SendSampleGroupAsync(
                session.Mediator,
                instrument.SerialNumber,
                analyticalRunId: newAnalyticalRunId.ToString(),
                assayRunId: newAssayRunId.ToString(),
                methodName: DefaultMethodName,
                testName: "c1",
                samples: [new SamplePoint("SID-new", ToMeasurementValue(100))],
                cancellationToken: TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        var assertDb = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await assertDb.AnalyticalRuns.AsNoTracking()
            .FirstAsync(a => a.Id == stalledRun.Id, TestContext.Current.CancellationToken);

        run.EndDateTime.Should().NotBeNull("stalled processing run should receive an end timestamp");
        run.RunStatus.Should().Be(RunStatus.ReadyWithoutTests, "no samples in the stalled run");
        run.CurveState.Should().Be(CurveState.Invalid, "waiting curve state should be invalidated");

        if (initialCurveSource == CurveSource.CurveFromPreviousAssay)
        {
            run.CurveId.Should().BeNull("CurveId must be cleared when source was CurveFromPreviousAssay");
            run.CurveSource.Should().Be(CurveSource.Missing, "CurveSource must be reset to Missing");
        }
    }

}
