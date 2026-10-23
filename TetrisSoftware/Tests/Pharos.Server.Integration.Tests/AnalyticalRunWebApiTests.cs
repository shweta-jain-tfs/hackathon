using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Queries;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetAnalyticalRuns;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class AnalyticalRunWebApiTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{

    [Fact]
    public async Task GetAnalyticalRunsWithCorrectStatus()
    {
        var dbContext = ServiceProvider.GetService<IResultDbContext>()!;
        var client = Factory.HttpClient;
        var methodName = "sIgE";
        var instrumentNumber = "SEED_001";

        //Seed db with different combinations of statuses for Analytical runs

        //No issues, not approved, not in processing
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.NoIssue,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            QualityControlIssue = AnalyticalRunIssue.NoIssue,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Ready,
            TestResults =
            [
                new SampleTestResult
                {
                    InstrumentNumber = instrumentNumber,
                    MethodName = methodName,
                    TestName = "f1",
                    TestOrderId = Guid.NewGuid(),
                    Status = TestResultStatus.PendingApproval
                }
            ]
        });

        //No tests in run
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.NoIssue,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            QualityControlIssue = AnalyticalRunIssue.NoIssue,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.ReadyWithoutTests
        });

        //Processing
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.NoIssue,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            QualityControlIssue = AnalyticalRunIssue.NoIssue,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        //Test issue
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            QualityControlIssue = AnalyticalRunIssue.NoIssue,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        //QC flagged
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            QualityControlIssue = AnalyticalRunIssue.Warning,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        //CC flagged
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.Warning,
            QualityControlIssue = AnalyticalRunIssue.Warning,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        // Calibration flagged
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.Warning,
            QualityControlIssue = AnalyticalRunIssue.Warning,
            CalibrationCurveIssue = AnalyticalRunIssue.Warning,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        // QC failed
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.NoIssue,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            QualityControlIssue = AnalyticalRunIssue.Error,
            CalibrationCurveIssue = AnalyticalRunIssue.NoIssue,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        // CC failed
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.Error,
            QualityControlIssue = AnalyticalRunIssue.Error,
            CalibrationCurveIssue = AnalyticalRunIssue.Warning,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        // Calibration failed
        dbContext.AnalyticalRuns.Add(new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = methodName,
            InstrumentNumber = instrumentNumber,
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.Error,
            QualityControlIssue = AnalyticalRunIssue.Error,
            CalibrationCurveIssue = AnalyticalRunIssue.Error,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Processing
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uri = new Uri(Factory.Server.BaseAddress, "api/results/analyticalRuns?Count=10");
        var result = await client.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        //Evaluate according to status prioritization 
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(10);
        result!.Items[0].IssueStatus.Should().Be(IssueStatus.PendingApproval);
        result!.Items[2].IssueStatus.Should().Be(IssueStatus.NoIssue);
        result!.Items[3].IssueStatus.Should().Be(IssueStatus.FailedTests);
        result!.Items[4].IssueStatus.Should().Be(IssueStatus.QualityControlFlagged);
        result!.Items[5].IssueStatus.Should().Be(IssueStatus.CurveControlFlagged);
        result!.Items[6].IssueStatus.Should().Be(IssueStatus.CalibrationFlagged);
        result!.Items[7].IssueStatus.Should().Be(IssueStatus.QualityControlFailed);
        result!.Items[8].IssueStatus.Should().Be(IssueStatus.CurveControlFailed);
        result!.Items[9].IssueStatus.Should().Be(IssueStatus.CalibrationFailed);
    }

    [Fact]
    public async Task GetAnalyticalRuns_WhenRunHasMultipleIssues_ReturnsAllIssuesInPriorityOrder()
    {
        var dbContext = ServiceProvider.GetService<IResultDbContext>()!;
        var client = Factory.HttpClient;

        var multiIssueRun = new AnalyticalRun
        {
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            MethodName = "sIgE",
            InstrumentNumber = "SEED_MULTI_001",
            TestIssue = AnalyticalRunIssue.Warning,
            CurveControlIssue = AnalyticalRunIssue.Error,
            QualityControlIssue = AnalyticalRunIssue.Warning,
            CalibrationCurveIssue = AnalyticalRunIssue.Warning,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            RunStatus = RunStatus.Ready,
            TestResults =
            [
                new SampleTestResult
                {
                    InstrumentNumber = "SEED_MULTI_001",
                    MethodName = "sIgE",
                    TestName = "f1",
                    TestOrderId = Guid.NewGuid(),
                    Status = TestResultStatus.PendingApproval
                },
                new SampleTestResult
                {
                    InstrumentNumber = "SEED_MULTI_001",
                    MethodName = "sIgE",
                    TestName = "f2",
                    TestOrderId = Guid.NewGuid(),
                    Status = TestResultStatus.ApprovedPendingReflex
                }
            ]
        };

        dbContext.AnalyticalRuns.Add(multiIssueRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var uri = new Uri(Factory.Server.BaseAddress, "api/results/analyticalRuns?InstrumentNumber=SEED_MULTI_001&Count=10");
        var result = await client.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        var run = result.Items.Single();
        run.IssueStatus.Should().Be(IssueStatus.CurveControlFailed);
        run.Issues.Should().Equal(
            IssueStatus.CurveControlFailed,
            IssueStatus.CalibrationFlagged,
            IssueStatus.QualityControlFlagged,
            IssueStatus.FailedTests,
            IssueStatus.PendingApproval,
            IssueStatus.ApprovedPendingReflex);
    }

    // -------------------------------------------------------------------------
    // Status filter tests
    //
    // Dataset seeded by SeedStatusFilterRunsAsync:
    //  RunStatus           ApprovalStatus   → StateStatus
    //  Ready               NotApproved      → Ready
    //  Aborted             NotApproved      → Ready      (Aborted has no distinct StateStatus)
    //  Processing          NotApproved      → Processing
    //  ReadyWithoutTests   NotApproved      → ReadyWithoutTests
    //  Ready               Approved         → Approved
    //  Ready               Rejected         → Rejected
    //  Processing          Approved         → Approved   (ApprovalStatus wins over RunStatus)
    //  Aborted             Rejected         → Rejected   (ApprovalStatus wins over RunStatus)
    // -------------------------------------------------------------------------

    private async Task SeedStatusFilterRunsAsync()
    {
        var dbContext = ServiceProvider.GetService<IResultDbContext>()!;

        var runs = new (RunStatus RunStatus, ResultApprovalStatus ApprovalStatus)[]
        {
            (RunStatus.Ready,             ResultApprovalStatus.NotApproved),
            (RunStatus.Aborted,           ResultApprovalStatus.NotApproved),
            (RunStatus.Processing,        ResultApprovalStatus.NotApproved),
            (RunStatus.ReadyWithoutTests, ResultApprovalStatus.NotApproved),
            (RunStatus.Ready,             ResultApprovalStatus.Approved),
            (RunStatus.Ready,             ResultApprovalStatus.Rejected),
            (RunStatus.Processing,        ResultApprovalStatus.Approved),
            (RunStatus.Aborted,           ResultApprovalStatus.Rejected),
        };

        foreach (var (runStatus, approvalStatus) in runs)
        {
            dbContext.AnalyticalRuns.Add(new AnalyticalRun
            {
                InstrumentAnalyticalRunId = Guid.NewGuid(),
                MethodName = "sIgE",
                InstrumentNumber = "SEED_001",
                RunStatus = runStatus,
                ApprovalStatus = approvalStatus,
            });
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetAnalyticalRuns_FilterByReady_ReturnsOnlyReadyStateRuns()
    {
        await SeedStatusFilterRunsAsync();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/analyticalRuns?Status={nameof(StateStatus.Ready)}&Count=100");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result!.TotalCount.Should().Be(3);
        result.Items.Should().OnlyContain(x => x.StateStatus == StateStatus.Ready);
    }

    [Fact]
    public async Task GetAnalyticalRuns_FilterByProcessing_ReturnsOnlyProcessingStateRuns()
    {
        await SeedStatusFilterRunsAsync();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/analyticalRuns?Status={nameof(StateStatus.Processing)}&Count=100");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result!.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(x => x.StateStatus == StateStatus.Processing);
    }

    [Fact]
    public async Task GetAnalyticalRuns_FilterByApproved_ReturnsOnlyApprovedStateRuns()
    {
        // Run with RunStatus.Processing and ApprovalStatus.Approved must be included — ApprovalStatus wins
        await SeedStatusFilterRunsAsync();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/analyticalRuns?Status={nameof(StateStatus.Approved)}&Count=100");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result!.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(x => x.StateStatus == StateStatus.Approved);
    }

    [Fact]
    public async Task GetAnalyticalRuns_FilterByRejected_ReturnsOnlyRejectedStateRuns()
    {
        // Run with RunStatus.Aborted and ApprovalStatus.Rejected must be included — ApprovalStatus wins
        await SeedStatusFilterRunsAsync();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/analyticalRuns?Status={nameof(StateStatus.Rejected)}&Count=100");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result!.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(x => x.StateStatus == StateStatus.Rejected);
    }

    [Fact]
    public async Task GetAnalyticalRuns_FilterByReadyAndProcessing_ReturnsBothGroups()
    {
        await SeedStatusFilterRunsAsync();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/analyticalRuns?Status={nameof(StateStatus.Ready)}&Status={nameof(StateStatus.Processing)}&Count=100");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result!.TotalCount.Should().Be(4);
        result.Items.Should().OnlyContain(x => x.StateStatus == StateStatus.Ready || x.StateStatus == StateStatus.Processing);
    }

    [Fact]
    public async Task GetAnalyticalRuns_FilterByApprovedAndReady_ExcludesRunsWhoseRunStatusMatchesButApprovalStatusOverrides()
    {
        // Runs with RunStatus.Ready/Aborted but ApprovalStatus.Rejected must NOT appear
        await SeedStatusFilterRunsAsync();
        var uri = new Uri(Factory.Server.BaseAddress, $"api/results/analyticalRuns?Status={nameof(StateStatus.Approved)}&Status={nameof(StateStatus.Ready)}&Count=100");

        var result = await Factory.HttpClient.GetFromJsonAsync<PartialQueryResult<AnalyticalRunDto>>(uri, TestContext.Current.CancellationToken);

        result!.TotalCount.Should().Be(5);
        result.Items.Should().OnlyContain(x => x.StateStatus == StateStatus.Approved || x.StateStatus == StateStatus.Ready);
    }
}