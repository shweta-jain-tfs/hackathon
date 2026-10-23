using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.Integration.Tests.CalibratorCurveIntegration;

/// <summary>
/// Verifies task-card behaviour when a flagged CC run is resolved.
///
/// <para><b>Notification-chain scoping rule</b><br/>
/// <see cref="AcceptCurveFromFlaggedCurveControlCommand"/> calls
/// <c>mediator.Publish(AnalyticalRunCalculationCurveUpdated, TestContext.Current.CancellationToken)</c> directly inside its handler.
/// That publish goes through the scoped <c>DelayedPublisher</c>, which only flushes
/// notifications into the background channel when the DI scope is disposed.
/// Every test therefore wraps all mediator activity in an explicit
/// <c>await using (var session = ...) { }</c> block and calls
/// <see cref="DatabaseTestBase{TWaf}.WaitForNotificationsAsync"/> <em>after</em>
/// that block closes.</para>
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class FlaggedCcAcceptCurveTaskCardTests(IntegrationTestsWaf factory)
    : CalibratorCurveIntegrationTestBase(factory)
{
    private const string _ccPointName = "CC-1";
    private const string _ccTestName = "a-IgE";

    // ── Scenario A ────────────────────────────────────────────────────────────
    // User accepts the flagged curve → run is committed, task card is removed.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptFlaggedCurve_Should_CommitRun_And_RemoveResultIssueTaskCard()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var taskCardService = ServiceProvider.GetRequiredService<ITaskCardService>();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            runId = await SeedFlaggedRunAsync(session, instrument.SerialNumber);

            // Pre-condition: task card must be present before the user acts.
            taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
                .Should().BeTrue("a Warning task card must exist while the CC is flagged");

            await session.Mediator.Send(
                new AcceptCurveFromFlaggedCurveControlCommand(runId), TestContext.Current.CancellationToken);

        } // Scope disposal flushes AnalyticalRunCalculationCurveUpdated into the background channel.

        await WaitForNotificationsAsync();

        var db = ServiceProvider.GetRequiredService<IResultDbContext>();
        var run = await db.AnalyticalRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

        run.CurveState.Should().Be(CurveState.Frozen,
            "accepting a flagged curve must commit the run");

        taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
            .Should().BeFalse("the CalibrationIssue task card must be removed once the run is committed");
    }

    // ── Scenario C ────────────────────────────────────────────────────────────
    // Flagged curve accepted → run committed. Then failed CC arrives.
    // Committed run should reject post-commitment CC updates (frozen state).
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostCommitmentFailedCc_Should_NotModifyCommittedRun()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var taskCardService = ServiceProvider.GetRequiredService<ITaskCardService>();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            // Setup: Flagged run
            runId = await SeedFlaggedRunAsync(session, instrument.SerialNumber);
            var run = await session.ResultDbContext.AnalyticalRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

            // Pre-condition: run is Invalid with Flagged curve
            run.CurveState.Should().Be(CurveState.Invalid);
            run.CurveControlTotalJudgment.Should().Be(CurveControlJudgmentResult.Flagged);

            // User accepts the flagged curve
            await session.Mediator.Send(
                new AcceptCurveFromFlaggedCurveControlCommand(runId), TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        var db = ServiceProvider.GetRequiredService<IResultDbContext>();
        var committedRun = await db.AnalyticalRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

        // Verify run is committed and frozen
        committedRun.CurveState.Should().Be(CurveState.Frozen);
        committedRun.FrozenAt.Should().NotBeNull("run must record commitment timestamp");
        committedRun.CurveControlIssue.Should().Be(AnalyticalRunIssue.NoIssue, "issues cleared at commitment");
        var issuesBeforeFailedCc = committedRun.CurveControlIssue;
        var judgmentBeforeFailedCc = committedRun.CurveControlTotalJudgment;

        // Act: Instrument sends a failed CC after commitment.
        // This must exercise CurveControlResultGroupReadyEvent and hit the committed-run guard.
        await using (var session2 = CreateSession())
        {
            SetInstrumentInAssayMode(session2.Scope, instrument.SerialNumber);

            await SendBlankAsync(session2.Mediator, instrument.SerialNumber);
            await SendCurveControlGroupAsync(
                session2.Mediator,
                instrument.SerialNumber,
                analyticalRunId: committedRun.InstrumentAnalyticalRunId.ToString(),
                assayRunId: committedRun.InstrumentRunId.ToString(),
                testName: _ccTestName,
                curveControls:
                [
                    new CurveControlPoint(
                        Name: _ccPointName,
                        FirstMeasurementValue: 5000)
                ],
                methodName: DefaultMethodName,
                cancellationToken: TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        var runAfterFailedCc = await db.AnalyticalRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

        runAfterFailedCc.CurveState.Should()
            .Be(CurveState.Frozen,
                "committed state is terminal and cannot change");

        runAfterFailedCc.CurveControlIssue.Should()
            .Be(issuesBeforeFailedCc,
                "post-commitment events must not modify run's issues (guard prevents it)");

        runAfterFailedCc.CurveControlTotalJudgment.Should()
            .Be(judgmentBeforeFailedCc,
                "post-commitment events must not change run's CC judgment");

        taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
            .Should().BeFalse("task card should remain absent (was removed at commitment)");
    }

    // ── Shared arrange helper ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies task-card atomicity when same-state CC judgment updates issue severity.
    /// Scenario: Run in Invalid state with Flagged CC (Warning issue).
    /// Then another CC arrives, also flagged but more critical (becomes Error).
    /// The state stays Invalid, but the issue escalates → task card must update.
    /// </summary>
    [Fact]
    public async Task SameStateIssueEscalation_Should_UpdateTaskCardState()
    {
        var (_, instrument) = await Factory.AddInstrumentConnection();
        var taskCardService = ServiceProvider.GetRequiredService<ITaskCardService>();
        int runId;

        await using (var session = CreateSession())
        {
            SetInstrumentInAssayMode(session.Scope, instrument.SerialNumber);

            // Seed initial flagged run with Warning
            runId = await SeedFlaggedRunAsync(session, instrument.SerialNumber);
            var run = await session.ResultDbContext.AnalyticalRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

            // Pre-condition: run is Invalid with Warning issue and Warning task card
            run.CurveState.Should().Be(CurveState.Invalid);
            run.CurveControlIssue.Should().Be(AnalyticalRunIssue.Warning);
            taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
                .Should().BeTrue("Warning task card must exist for flagged CC");

            // Simulate a second CC arriving that escalates to Error (e.g., multiple replicates out of range)
            run.CurveControlTotalJudgment = CurveControlJudgmentResult.NotOK;

            // Publish the issue-driven event: the handler escalates CurveControlIssue to Error.
            run.AddDomainEvent(new UpdateAnalyticalRunQualityIssue(
                run,
                CurveIssue: null,
                CurveControlIssue: AnalyticalRunIssue.Error,
                QualityControlIssue: null,
                TestIssue: null));

            await session.ResultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await WaitForNotificationsAsync();

        var db = ServiceProvider.GetRequiredService<IResultDbContext>();
        var runState = await db.AnalyticalRuns.FirstAsync(r => r.Id == runId, TestContext.Current.CancellationToken);

        // Verify state unchanged
        runState.CurveState.Should().Be(CurveState.Invalid);

        // Verify issue escalated
        runState.CurveControlIssue.Should().Be(AnalyticalRunIssue.Error,
            "second CC arrival with worse judgment should escalate issue");

        // Verify task card updated to Error state (not still Warning)
        taskCardService.Exists(TaskCardType.CalibrationIssue, instrument.SerialNumber)
            .Should().BeTrue("task card must still exist");

        // The task card should now reflect Error state, not Warning
        var cards = taskCardService.GetTaskCardsForInstrument(instrument.SerialNumber);
        var resultIssueCard = cards.FirstOrDefault(c => c.CardType == TaskCardType.CalibrationIssue);
        resultIssueCard?.CurrentCardState.Should().Be(TaskCardState.Error,
            "task card state must escalate from Warning to Error when issue escalates");
    }

    // ── Shared arrange helper ─────────────────────────────────────────────────

    /// <summary>
    /// Seeds an analytical run in the state produced by the CC evaluation pipeline
    /// when a CC result is judged <see cref="CurveControlJudgmentResult.Flagged"/>:
    /// <list type="bullet">
    ///   <item>OK calibration curve assigned as the calculation curve.</item>
    ///   <item><see cref="CurveState.Invalid"/> — blocked from recalculating.</item>
    ///   <item><see cref="AnalyticalRunIssue.Warning"/> on <c>CurveControlIssue</c>.</item>
    ///   <item>A matching <see cref="TaskCardState.Warning"/> task card already seeded.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// This helper performs only direct DB writes and synchronous <c>Send</c> calls.
    /// It must <b>not</b> call <see cref="DatabaseTestBase{TWaf}.WaitForNotificationsAsync"/>
    /// — the session scope is still alive here so <c>DelayedPublisher</c> has not yet flushed.
    /// The caller is responsible for disposing the session and then waiting.
    /// </remarks>
    private static async Task<int> SeedFlaggedRunAsync(CalibrationTestSession session, string serialNumber)
    {
        await SeedMethodForCurveAndCcAsync(session.ParametersDbContext);

        var data = await CreateTestData(serialNumber)
            .WithAnalyticalRun(issue: AnalyticalRunIssue.Warning)
            .WithCurve(CurveJudgment.OK)
            .WithCurveControlResult(_ccPointName, testName: _ccTestName, analyticalRunIndex: 0)
            .BuildAsync(session.ResultDbContext);

        var run = data.AnalyticalRuns[0];
        run.CurveId = data.Curves[0].Id;
        run.CurveSource = CurveSource.CurveFromCurrentAssay;
        run.CurveControlTotalJudgment = CurveControlJudgmentResult.Flagged;
        run.CurveState = CurveState.Invalid;
        run.CurveControlIssue = AnalyticalRunIssue.Warning;

        await session.ResultDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Seed the Warning task card directly — mirrors what the CC evaluation
        // pipeline emits when a CC is first judged Flagged.
        await session.Mediator.Send(
            new TaskCardCommand(TaskCardType.CalibrationIssue, TaskCardState.Warning, serialNumber),
            TestContext.Current.CancellationToken);

        return run.Id;
    }
}
