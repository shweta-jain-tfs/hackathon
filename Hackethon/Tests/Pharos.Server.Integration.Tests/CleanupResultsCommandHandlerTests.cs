using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Database;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Jobs;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using Quartz;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class CleanupResultsCommandHandlerTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Handle_WhenNoOldAnalyticalRuns_ShouldNotDeleteAnything()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-5),
            EndDateTime = DateTimeOffset.Now.AddDays(-4),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenAnalyticalRunIsNotReady_ShouldNotDeleteIt()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Processing,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenAnalyticalRunHasNoEndDateTime_ShouldNotDeleteIt()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = null,
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenNotAllSampleResultsAreReported_ShouldNotDeleteAnalyticalRun()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reportedSample = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var pendingSample = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.PendingApproval,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(reportedSample);
        dbContext.TestResults.Add(pendingSample);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.AnalyticalRuns.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenEligibleAnalyticalRunExists_ShouldDeleteItAndSampleResults()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;
        var sampleResultId = sampleResult.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeFalse();

        var sampleResultExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == sampleResultId, TestContext.Current.CancellationToken);
        sampleResultExists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenAnalyticalRunHasQualityResults_ShouldRemoveLinkButNotDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var calibratorResult = new CalibratorTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Calibrator,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(calibratorResult);
        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;
        var calibratorId = calibratorResult.Id;
        var sampleId = sampleResult.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeFalse();

        var sampleResultExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == sampleId, TestContext.Current.CancellationToken);
        sampleResultExists.Should().BeFalse();

        var calibratorExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == calibratorId, TestContext.Current.CancellationToken);
        calibratorExists.Should().BeTrue();

        var calibrator = await dbContext.TestResults.FindAsync([calibratorId], TestContext.Current.CancellationToken);
        calibrator!.AnalyticalRunId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenCurvesExist_ShouldNotDeleteCurves()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curve = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        dbContext.Curves.Add(curve);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curveId = curve.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var curveStillExists = await dbContext.Curves.AnyAsync(c => c.Id == curveId, TestContext.Current.CancellationToken);
        curveStillExists.Should().BeTrue();

        var updatedCurve = await dbContext.Curves.FindAsync([curveId], TestContext.Current.CancellationToken);
        updatedCurve.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenMultipleAnalyticalRuns_ShouldOnlyDeleteEligibleOnes()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        // Should be deleted: old, ready, all reported
        var oldRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = [],
            InstrumentAnalyticalRunId = Guid.NewGuid()
        };

        // Should NOT be deleted: too recent
        var recentRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-5),
            EndDateTime = DateTimeOffset.Now.AddDays(-5),
            RunStatus = RunStatus.Ready,
            TestResults = [],
            InstrumentAnalyticalRunId = Guid.NewGuid()
        };

        dbContext.AnalyticalRuns.Add(oldRun);
        dbContext.AnalyticalRuns.Add(recentRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldSample = new SampleTestResult
        {
            AnalyticalRunId = oldRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var recentSample = new SampleTestResult
        {
            AnalyticalRunId = recentRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-5),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(oldSample);
        dbContext.TestResults.Add(recentSample);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldRunId = oldRun.Id;
        var recentRunId = recentRun.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var oldRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == oldRunId, TestContext.Current.CancellationToken);
        oldRunExists.Should().BeFalse();

        var recentRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == recentRunId, TestContext.Current.CancellationToken);
        recentRunExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAnalyticalRunHasNoTestResults_ShouldStillDeleteIfEligible()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithCustomOlderThanDays_ShouldUseCorrectCompareDate()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-40),
            EndDateTime = DateTimeOffset.Now.AddDays(-40),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-40),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;

        // Using 60 days - the analytical run should NOT be deleted (only 40 days old)
        var command = new CleanupResultsCommand(60);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAnalyticalRunHasMixedTestResults_ShouldHandleCorrectly()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResult1 = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var sampleResult2 = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var calibratorResult = new CalibratorTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Calibrator,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var qcResult = new QualityControlTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.QualityControl,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult1);
        dbContext.TestResults.Add(sampleResult2);
        dbContext.TestResults.Add(calibratorResult);
        dbContext.TestResults.Add(qcResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;
        var sample1Id = sampleResult1.Id;
        var sample2Id = sampleResult2.Id;
        var calibratorId = calibratorResult.Id;
        var qcId = qcResult.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeFalse();

        // Sample results should be removed
        var sample1Exists = await dbContext.TestResults.AnyAsync(tr => tr.Id == sample1Id, TestContext.Current.CancellationToken);
        sample1Exists.Should().BeFalse();

        var sample2Exists = await dbContext.TestResults.AnyAsync(tr => tr.Id == sample2Id, TestContext.Current.CancellationToken);
        sample2Exists.Should().BeFalse();

        // Quality results should have link removed but not be deleted
        var calibratorExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == calibratorId, TestContext.Current.CancellationToken);
        calibratorExists.Should().BeTrue();
        var calibrator = await dbContext.TestResults.FindAsync([calibratorId], TestContext.Current.CancellationToken);
        calibrator!.AnalyticalRunId.Should().BeNull();

        var qcExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == qcId, TestContext.Current.CancellationToken);
        qcExists.Should().BeTrue();
        var qc = await dbContext.TestResults.FindAsync([qcId], TestContext.Current.CancellationToken);
        qc!.AnalyticalRunId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTestOrdersStillExist_ShouldNotDeleteAnalyticalRun()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testOrderId = Guid.NewGuid();

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = testOrderId,
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var requestDbContext = ServiceProvider.GetRequiredService<Pharos.Server.Request.Application.Interfaces.IRequestDbContext>();
        var testOrder = new Pharos.Server.Request.Domain.TestOrder
        {
            Uid = testOrderId,
            MethodName = "sIgE",
            TestName = "f1",
            Request = new Pharos.Server.Request.Domain.Request
            {
                SampleId = "Sample001",
            }
        };
        requestDbContext.TestOrders.Add(testOrder);
        await requestDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMultipleCurvesExist_ShouldNotDeleteCurves()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curve1 = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        var curve2 = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        var curve3 = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.Curves.Add(curve1);
        dbContext.Curves.Add(curve2);
        dbContext.Curves.Add(curve3);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curve1Id = curve1.Id;
        var curve2Id = curve2.Id;
        var curve3Id = curve3.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedCurve1 = await dbContext.Curves.FindAsync([curve1Id], TestContext.Current.CancellationToken);
        updatedCurve1.Should().NotBeNull();

        var updatedCurve2 = await dbContext.Curves.FindAsync([curve2Id], TestContext.Current.CancellationToken);
        updatedCurve2.Should().NotBeNull();

        var updatedCurve3 = await dbContext.Curves.FindAsync([curve3Id], TestContext.Current.CancellationToken);
        updatedCurve3.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenAnalyticalRunHasOnlyQualityResults_ShouldStillDeleteIfEligible()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var calibratorResult = new CalibratorTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Calibrator,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var qcResult = new QualityControlTestResult
        {
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.QualityControl,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(calibratorResult);
        dbContext.TestResults.Add(qcResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRunId = analyticalRun.Id;
        var calibratorId = calibratorResult.Id;
        var qcId = qcResult.Id;

        var command = new CleanupResultsCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var analyticalRunExists = await dbContext.AnalyticalRuns.AnyAsync(ar => ar.Id == analyticalRunId, TestContext.Current.CancellationToken);
        analyticalRunExists.Should().BeFalse();

        var calibratorExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == calibratorId, TestContext.Current.CancellationToken);
        calibratorExists.Should().BeTrue();
        var calibrator = await dbContext.TestResults.FindAsync([calibratorId], TestContext.Current.CancellationToken);
        calibrator!.AnalyticalRunId.Should().BeNull();

        var qcExists = await dbContext.TestResults.AnyAsync(tr => tr.Id == qcId, TestContext.Current.CancellationToken);
        qcExists.Should().BeTrue();
        var qc = await dbContext.TestResults.FindAsync([qcId], TestContext.Current.CancellationToken);
        qc!.AnalyticalRunId.Should().BeNull();
    }

    [Fact]
    public async Task Execute_WhenAutomaticDeleteResultsEnabled_ShouldDeleteEligibleRequestResultDataAcrossTables()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = false,
            DeleteResultsAutomatically = true,
            DeleteResultsOlderThanDays = 30,
            DeleteQualityAutomatically = false
        });

        var oldAnalyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = [],
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.AnalyticalRuns.Add(oldAnalyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResultOne = new SampleTestResult
        {
            AnalyticalRunId = oldAnalyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var sampleResultTwo = new SampleTestResult
        {
            AnalyticalRunId = oldAnalyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f2",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.AddRange(sampleResultOne, sampleResultTwo);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var replicateOne = new SampleTestReplicate
        {
            TestResultId = sampleResultOne.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        var replicateTwo = new SampleTestReplicate
        {
            TestResultId = sampleResultTwo.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        dbContext.TestReplicates.AddRange(replicateOne, replicateTwo);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestReplicateErrors.Add(new TestReplicateError
        {
            TestReplicateId = replicateOne.Id,
            ErrorCode = 101
        });

        dbContext.TestReplicateReagents.Add(new TestReplicateReagent
        {
            TestReplicateId = replicateOne.Id,
            ReagentType = ArticleType.ImmunoCapCarrier,
            Barcode = "BC123456789",
            Udi = "UDI123456789",
            SerialNumber = "SN000001",
            CellPosition = 1,
            LotNumber = "LOT01",
            Expired = false
        });

        var unrelatedCurve = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            FirstDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(-70)),
            LastDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.Curves.Add(unrelatedCurve);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var runId = oldAnalyticalRun.Id;
        var resultOneId = sampleResultOne.Id;
        var resultTwoId = sampleResultTwo.Id;
        var replicateOneId = replicateOne.Id;
        var replicateTwoId = replicateTwo.Id;
        var curveId = unrelatedCurve.Id;

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.AnalyticalRuns.AnyAsync(x => x.Id == runId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == resultOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == resultTwoId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == replicateOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == replicateTwoId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicateErrors.AnyAsync(x => x.TestReplicateId == replicateOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicateReagents.AnyAsync(x => x.TestReplicateId == replicateOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.Curves.AnyAsync(x => x.Id == curveId, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3593")]
    public async Task Execute_WhenAutomaticDeleteResultsEnabled_ShouldDeleteEligibleRequestResultDataAcrossTables_MR3593()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = false,
            DeleteResultsAutomatically = true,
            DeleteResultsOlderThanDays = 30,
            DeleteQualityAutomatically = false
        });

        var oldAnalyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = [],
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.AnalyticalRuns.Add(oldAnalyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResultOne = new SampleTestResult
        {
            AnalyticalRunId = oldAnalyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var sampleResultTwo = new SampleTestResult
        {
            AnalyticalRunId = oldAnalyticalRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f2",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.AddRange(sampleResultOne, sampleResultTwo);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var replicateOne = new SampleTestReplicate
        {
            TestResultId = sampleResultOne.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        var replicateTwo = new SampleTestReplicate
        {
            TestResultId = sampleResultTwo.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        dbContext.TestReplicates.AddRange(replicateOne, replicateTwo);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestReplicateErrors.Add(new TestReplicateError
        {
            TestReplicateId = replicateOne.Id,
            ErrorCode = 101
        });

        dbContext.TestReplicateReagents.Add(new TestReplicateReagent
        {
            TestReplicateId = replicateOne.Id,
            ReagentType = ArticleType.ImmunoCapCarrier,
            Barcode = "BC123456789",
            Udi = "UDI123456789",
            SerialNumber = "SN000001",
            CellPosition = 1,
            LotNumber = "LOT01",
            Expired = false
        });

        var unrelatedCurve = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            FirstDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(-70)),
            LastDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.Curves.Add(unrelatedCurve);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var runId = oldAnalyticalRun.Id;
        var resultOneId = sampleResultOne.Id;
        var resultTwoId = sampleResultTwo.Id;
        var replicateOneId = replicateOne.Id;
        var replicateTwoId = replicateTwo.Id;
        var curveId = unrelatedCurve.Id;

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.AnalyticalRuns.AnyAsync(x => x.Id == runId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == resultOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == resultTwoId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == replicateOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == replicateTwoId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicateErrors.AnyAsync(x => x.TestReplicateId == replicateOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicateReagents.AnyAsync(x => x.TestReplicateId == replicateOneId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.Curves.AnyAsync(x => x.Id == curveId, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3593")]
    public async Task Handle_WhenRunStatusIsProcessingAndSamplesReported_ShouldNotDeleteRequestResultData()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var oldProcessingRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Processing,
            TestResults = [],
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.AnalyticalRuns.Add(oldProcessingRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sampleResult = new SampleTestResult
        {
            AnalyticalRunId = oldProcessingRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var replicate = new SampleTestReplicate
        {
            TestResultId = sampleResult.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        dbContext.TestReplicates.Add(replicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var runId = oldProcessingRun.Id;
        var sampleId = sampleResult.Id;
        var replicateId = replicate.Id;

        // Act
        await mediator.Send(new CleanupResultsCommand(30), TestContext.Current.CancellationToken);

        // Assert
        (await dbContext.AnalyticalRuns.AnyAsync(x => x.Id == runId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestResults.AnyAsync(x => x.Id == sampleId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == replicateId, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3593")]
    public async Task Handle_WhenNotAllSampleResultsAreReported_ShouldNotDeleteRequestResultData()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var oldReadyRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = [],
            InstrumentAnalyticalRunId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.AnalyticalRuns.Add(oldReadyRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reportedSample = new SampleTestResult
        {
            AnalyticalRunId = oldReadyRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.Reported,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f1",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        var pendingSample = new SampleTestResult
        {
            AnalyticalRunId = oldReadyRun.Id,
            ResultType = TestResultType.Sample,
            Status = TestResultStatus.PendingApproval,
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            MethodName = "sIgE",
            TestName = "f2",
            TestOrderId = Guid.NewGuid(),
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.AddRange(reportedSample, pendingSample);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reportedReplicate = new SampleTestReplicate
        {
            TestResultId = reportedSample.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        var pendingReplicate = new SampleTestReplicate
        {
            TestResultId = pendingSample.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70)
        };

        dbContext.TestReplicates.AddRange(reportedReplicate, pendingReplicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var runId = oldReadyRun.Id;
        var reportedId = reportedSample.Id;
        var pendingId = pendingSample.Id;
        var reportedReplicateId = reportedReplicate.Id;
        var pendingReplicateId = pendingReplicate.Id;

        // Act
        await mediator.Send(new CleanupResultsCommand(30), TestContext.Current.CancellationToken);

        // Assert
        (await dbContext.AnalyticalRuns.AnyAsync(x => x.Id == runId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestResults.AnyAsync(x => x.Id == reportedId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestResults.AnyAsync(x => x.Id == pendingId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == reportedReplicateId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == pendingReplicateId, TestContext.Current.CancellationToken)).Should().BeTrue();
    }
}
