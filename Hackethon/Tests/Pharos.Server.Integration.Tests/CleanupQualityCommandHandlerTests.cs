using System.Globalization;
using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Database;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.Jobs;
using Pharos.Server.Parameters.Application.Interfaces;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Queries.GetTestResults;
using Pharos.Server.Result.Domain;
using Quartz;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class CleanupQualityCommandHandlerTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Handle_WhenNoOldTestResults_ShouldNotDeleteAnything()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var recentDate = now.Subtract(TimeSpan.FromDays(5));

        var calibratorResult = new CalibratorTestResult
        {
            MeasurementDateTime = recentDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.Calibrator,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(calibratorResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenOldCurveControlResultsExist_ShouldDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var curveControl = new CurveControlTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.CurveControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(curveControl);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenOldQualityControlResultsExist_ShouldDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(56));

        var qcResult = new QualityControlTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.QualityControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(qcResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenTestResultHasAnalyticalRunId_ShouldNotDeleteIt()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            TestResults = []
        };
        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var calibratorResult = new CalibratorTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Calibrator,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(calibratorResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenSampleTestResultsExist_ShouldNotDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var sampleResult = new SampleTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.Sample,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(sampleResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenOldBlanksExistButAreUsed_ShouldNotDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var blank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "BARCODE123",
            StopSolutionBarcode = "STOP123"
        };

        dbContext.Blanks.Add(blank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testResult = new SampleTestResult
        {
            MeasurementDateTime = now,
            ResultType = TestResultType.Sample,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };
        dbContext.TestResults.Add(testResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testReplicate = new SampleTestReplicate
        {
            TestResultId = testResult.Id,
            UsedBlankId = blank.Id,
            InstrumentReplicateId = Guid.NewGuid()
        };

        dbContext.TestReplicates.Add(testReplicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blankCountBefore = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var blankCountAfter = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);
        blankCountAfter.Should().Be(blankCountBefore);
    }

    [Fact]
    public async Task Handle_WhenOldUnusedBlanksExist_ShouldDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var usedBlank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "BARCODE123",
            StopSolutionBarcode = "STOP123"
        };

        var unusedBlank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "BARCODE456",
            StopSolutionBarcode = "STOP456"
        };

        dbContext.Blanks.Add(usedBlank);
        dbContext.Blanks.Add(unusedBlank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testResult = new SampleTestResult
        {
            MeasurementDateTime = now,
            ResultType = TestResultType.Sample,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };
        dbContext.TestResults.Add(testResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testReplicate = new SampleTestReplicate
        {
            TestResultId = testResult.Id,
            UsedBlankId = usedBlank.Id,
            InstrumentReplicateId = Guid.NewGuid()
        };

        dbContext.TestReplicates.Add(testReplicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blankCountBefore = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var blankCountAfter = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);
        blankCountAfter.Should().Be(blankCountBefore - 1);

        var remainingBlanks = await dbContext.Blanks.ToListAsync(TestContext.Current.CancellationToken);
        remainingBlanks.Should().HaveCount(1);
        remainingBlanks.First().Id.Should().Be(usedBlank.Id);
    }

    [Fact]
    public async Task Handle_WhenRecentBlanksExist_ShouldNotDeleteThem()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var recentDate = now.Subtract(TimeSpan.FromDays(9));

        var blank = new Blank
        {
            MeasurementTime = recentDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "BARCODE123",
            StopSolutionBarcode = "STOP123"
        };

        dbContext.Blanks.Add(blank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blankCountBefore = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var blankCountAfter = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);
        blankCountAfter.Should().Be(blankCountBefore);
    }

    [Fact]
    public async Task Handle_WithMixedScenario_ShouldDeleteOnlyEligibleRecords()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));
        var recentDate = now.Subtract(TimeSpan.FromDays(9));

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            TestResults = []
        };
        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Should NOT be deleted: has analytical run
        var linkedCalibrator = new CalibratorTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = analyticalRun.Id,
            ResultType = TestResultType.Calibrator,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001",
        };

        // Should NOT be deleted: recent
        var recentQc = new QualityControlTestResult
        {
            MeasurementDateTime = recentDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.QualityControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001",
        };

        // Should NOT be deleted: sample type
        var oldSample = new SampleTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.Sample,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001",
        };

        dbContext.TestResults.Add(linkedCalibrator);
        dbContext.TestResults.Add(recentQc);
        dbContext.TestResults.Add(oldSample);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldUnusedBlank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "UNUSED",
            StopSolutionBarcode = "STOPUNUSED"
        };

        var oldUsedBlank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "USED",
            StopSolutionBarcode = "STOPUSED"
        };

        var recentBlank = new Blank
        {
            MeasurementTime = recentDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "RECENT",
            StopSolutionBarcode = "STOPRECENT"
        };

        dbContext.Blanks.Add(oldUnusedBlank);
        dbContext.Blanks.Add(oldUsedBlank);
        dbContext.Blanks.Add(recentBlank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testReplicate = new SampleTestReplicate
        {
            TestResultId = oldSample.Id,
            UsedBlankId = oldUsedBlank.Id,
            InstrumentReplicateId = Guid.NewGuid()
        };

        dbContext.TestReplicates.Add(testReplicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testResultCountBefore = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        var blankCountBefore = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var testResultCountAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        testResultCountAfter.Should().Be(testResultCountBefore); // nothing deleted

        var blankCountAfter = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);
        blankCountAfter.Should().Be(blankCountBefore - 1); // Only oldUnusedBlank deleted

        var remainingTestResults = await dbContext.TestResults.ToListAsync(TestContext.Current.CancellationToken);
        remainingTestResults.Should().Contain(r => r.Id == linkedCalibrator.Id);
        remainingTestResults.Should().Contain(r => r.Id == recentQc.Id);
        remainingTestResults.Should().Contain(r => r.Id == oldSample.Id);

        var remainingBlanks = await dbContext.Blanks.ToListAsync(TestContext.Current.CancellationToken);
        remainingBlanks.Should().Contain(b => b.Id == oldUsedBlank.Id);
        remainingBlanks.Should().Contain(b => b.Id == recentBlank.Id);
        remainingBlanks.Should().NotContain(b => b.Id == oldUnusedBlank.Id);
    }

    [Fact]
    public async Task Handle_WhenNoBlanksToDelete_ShouldNotFail()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var recentDate = now.Subtract(TimeSpan.FromDays(5));

        var blank = new Blank
        {
            MeasurementTime = recentDate,
            InstrumentNumber = "TestInstrument",
            DevSolutionBarcode = "RECENT",
            StopSolutionBarcode = "STOPRECENT"
        };

        dbContext.Blanks.Add(blank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        var act = async () => await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WithCustomOlderThanDays_ShouldUseCorrectCompareDate()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var testDate = now.Subtract(TimeSpan.FromDays(40)); // 40 days old

        var calibratorResult = new CalibratorTestResult
        {
            MeasurementDateTime = testDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.Calibrator,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001",
        };

        dbContext.TestResults.Add(calibratorResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var countBefore = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);

        // Using 60 days - the test result should NOT be deleted (only 40 days old)
        var command = new CleanupQualityCommand(60);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var countAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_WhenEmptyDatabase_ShouldNotFail()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();

        var testResultCountBefore = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        var blankCountBefore = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);

        var command = new CleanupQualityCommand(30);

        // Act
        var act = async () => await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
        var testResultCountAfter = await dbContext.TestResults.CountAsync(TestContext.Current.CancellationToken);
        var blankCountAfter = await dbContext.Blanks.CountAsync(TestContext.Current.CancellationToken);
        testResultCountAfter.Should().Be(testResultCountBefore);
        blankCountAfter.Should().Be(blankCountBefore);
    }

    [Fact]
    public async Task Handle_WhenMultipleReplicatesUseTheSameBlank_ShouldNotDeleteBlank()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var sharedBlank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "SN001",
            DevSolutionBarcode = "SHARED",
            StopSolutionBarcode = "STOPSHARED"
        };

        dbContext.Blanks.Add(sharedBlank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testResult1 = new SampleTestResult
        {
            MeasurementDateTime = now,
            ResultType = TestResultType.Sample,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var testResult2 = new SampleTestResult
        {
            MeasurementDateTime = now,
            ResultType = TestResultType.Sample,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(testResult1);
        dbContext.TestResults.Add(testResult2);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var testReplicate1 = new SampleTestReplicate
        {
            TestResultId = testResult1.Id,
            UsedBlankId = sharedBlank.Id,
            InstrumentReplicateId = Guid.NewGuid()
        };

        var testReplicate2 = new SampleTestReplicate
        {
            TestResultId = testResult2.Id,
            UsedBlankId = sharedBlank.Id,
            InstrumentReplicateId = Guid.NewGuid()
        };

        var testReplicate3 = new SampleTestReplicate
        {
            TestResultId = testResult1.Id,
            UsedBlankId = sharedBlank.Id,
            InstrumentReplicateId = Guid.NewGuid()
        };

        dbContext.TestReplicates.Add(testReplicate1);
        dbContext.TestReplicates.Add(testReplicate2);
        dbContext.TestReplicates.Add(testReplicate3);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blankId = sharedBlank.Id;

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var blankExists = await dbContext.Blanks.AnyAsync(b => b.Id == blankId, TestContext.Current.CancellationToken);
        blankExists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAllQualityTypesExist_ShouldDeleteAllEligibleOnes()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var dateTime = ServiceProvider.GetRequiredService<IDateTime>();

        var now = dateTime.Now;
        var oldDate = now.Subtract(TimeSpan.FromDays(70));

        var curveControl = new CurveControlTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.CurveControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var qualityControl = new QualityControlTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.QualityControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(curveControl);
        dbContext.TestResults.Add(qualityControl);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curveControlId = curveControl.Id;
        var qualityControlId = qualityControl.Id;

        var command = new CleanupQualityCommand(30);

        // Act
        await mediator.Send(command, TestContext.Current.CancellationToken);

        // Assert
        var curveControlExists = await dbContext.TestResults.AnyAsync(x => x.Id == curveControlId, TestContext.Current.CancellationToken);
        curveControlExists.Should().BeFalse();

        var qualityControlExists = await dbContext.TestResults.AnyAsync(x => x.Id == qualityControlId, TestContext.Current.CancellationToken);
        qualityControlExists.Should().BeFalse();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5487")]
    public async Task Execute_WhenAutomaticDeleteQualityEnabled_ShouldDeleteUnreferencedQualityDataAcrossTables()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = false,
            DeleteResultsAutomatically = false,
            DeleteQualityAutomatically = true,
            DeleteQualityOlderThanDays = 30
        });

        var oldDate = DateTimeOffset.Now.AddDays(-70);

        var unreferencedCurve = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = oldDate,
            FirstDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
            LastDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.Curves.Add(unreferencedCurve);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldCurveControl = new CurveControlTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.CurveControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var oldQualityControl = new QualityControlTestResult
        {
            MeasurementDateTime = oldDate,
            AnalyticalRunId = null,
            ResultType = TestResultType.QualityControl,
            MethodName = "sIgE",
            TestName = "f2",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.AddRange(oldCurveControl, oldQualityControl);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ccReplicate = new CurveControlTestReplicate
        {
            TestResultId = oldCurveControl.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = oldDate
        };

        var qcReplicate = new QualityControlTestReplicate
        {
            TestResultId = oldQualityControl.Id,
            InstrumentReplicateId = Guid.NewGuid(),
            MeasurementDateTime = oldDate
        };

        dbContext.TestReplicates.AddRange(ccReplicate, qcReplicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.TestReplicateErrors.Add(new TestReplicateError
        {
            TestReplicateId = ccReplicate.Id,
            ErrorCode = 1101
        });

        dbContext.TestReplicateReagents.Add(new TestReplicateReagent
        {
            TestReplicateId = ccReplicate.Id,
            ReagentType = ArticleType.ImmunoCapCarrier,
            Barcode = "BCMR5487001",
            Udi = "UDIMR5487001",
            SerialNumber = "SN548701",
            CellPosition = 1,
            LotNumber = "L5487",
            Expired = false
        });

        var oldUnusedBlank = new Blank
        {
            MeasurementTime = oldDate,
            InstrumentNumber = "SN001",
            DevSolutionBarcode = "BLANK5487A",
            StopSolutionBarcode = "STOP5487A"
        };

        dbContext.Blanks.Add(oldUnusedBlank);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blankReplicate = new Pharos.Server.Result.Domain.BlankReplicate
        {
            BlankId = oldUnusedBlank.Id,
            MeasurementTime = oldDate,
            FluorometerValue = 1.23
        };

        dbContext.BlankReplicates.Add(blankReplicate);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        dbContext.BlankReplicateErrors.Add(new BlankReplicateError
        {
            BlankReplicateId = blankReplicate.Id,
            ErrorCode = 2101
        });

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curveId = unreferencedCurve.Id;
        var curveControlId = oldCurveControl.Id;
        var qualityControlId = oldQualityControl.Id;
        var ccReplicateId = ccReplicate.Id;
        var qcReplicateId = qcReplicate.Id;
        var blankId = oldUnusedBlank.Id;
        var blankReplicateId = blankReplicate.Id;

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.Curves.AnyAsync(x => x.Id == curveId, TestContext.Current.CancellationToken)).Should().BeFalse();

        (await dbContext.TestResults.AnyAsync(x => x.Id == curveControlId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == qualityControlId, TestContext.Current.CancellationToken)).Should().BeFalse();

        (await dbContext.TestReplicates.AnyAsync(x => x.Id == ccReplicateId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicates.AnyAsync(x => x.Id == qcReplicateId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicateErrors.AnyAsync(x => x.TestReplicateId == ccReplicateId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestReplicateReagents.AnyAsync(x => x.TestReplicateId == ccReplicateId, TestContext.Current.CancellationToken)).Should().BeFalse();

        (await dbContext.Blanks.AnyAsync(x => x.Id == blankId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.BlankReplicates.AnyAsync(x => x.Id == blankReplicateId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.BlankReplicateErrors.AnyAsync(x => x.BlankReplicateId == blankReplicateId, TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5487")]
    public async Task Execute_WhenCurveIsReferencedByAnalyticalRun_ShouldNotDeleteCurve()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = false,
            DeleteResultsAutomatically = false,
            DeleteQualityAutomatically = true,
            DeleteQualityOlderThanDays = 30
        });

        var oldCurve = new Curve
        {
            InstrumentNumber = "SN001",
            MethodName = "sIgE",
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            FirstDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(-90)),
            LastDayToUse = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = Guid.NewGuid()
        };

        dbContext.Curves.Add(oldCurve);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var analyticalRun = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            CurveId = oldCurve.Id,
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(analyticalRun);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var calibratorResult = new CalibratorTestResult
        {
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            AnalyticalRunId = analyticalRun.Id,
            CurveId = oldCurve.Id,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.Add(calibratorResult);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curveId = oldCurve.Id;
        var calibratorId = calibratorResult.Id;

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.Curves.AnyAsync(x => x.Id == curveId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestResults.AnyAsync(x => x.Id == calibratorId, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5487")]
    public async Task Execute_WhenQualityResultsArePartOfRun_ShouldNotDeleteCurveControlOrQualityControl()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = false,
            DeleteResultsAutomatically = false,
            DeleteQualityAutomatically = true,
            DeleteQualityOlderThanDays = 30
        });

        var run = new AnalyticalRun
        {
            MethodName = "sIgE",
            InstrumentNumber = "SN001",
            StartDateTime = DateTimeOffset.Now.AddDays(-70),
            EndDateTime = DateTimeOffset.Now.AddDays(-70),
            RunStatus = RunStatus.Ready,
            TestResults = []
        };

        dbContext.AnalyticalRuns.Add(run);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var oldCurveControl = new CurveControlTestResult
        {
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            AnalyticalRunId = run.Id,
            ResultType = TestResultType.CurveControl,
            MethodName = "sIgE",
            TestName = "f1",
            InstrumentNumber = "SN001"
        };

        var oldQualityControl = new QualityControlTestResult
        {
            MeasurementDateTime = DateTimeOffset.Now.AddDays(-70),
            AnalyticalRunId = run.Id,
            ResultType = TestResultType.QualityControl,
            MethodName = "sIgE",
            TestName = "f2",
            InstrumentNumber = "SN001"
        };

        dbContext.TestResults.AddRange(oldCurveControl, oldQualityControl);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var curveControlId = oldCurveControl.Id;
        var qualityControlId = oldQualityControl.Id;

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert
        (await dbContext.TestResults.AnyAsync(x => x.Id == curveControlId, TestContext.Current.CancellationToken)).Should().BeTrue();
        (await dbContext.TestResults.AnyAsync(x => x.Id == qualityControlId, TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5487")]
    public async Task E2E_ApiShouldReflectAutomaticQualityCleanupResultsWithoutUi()
    {
        // Arrange
        var dbContext = ServiceProvider.GetRequiredService<IResultDbContext>();
        var parametersDbContext = ServiceProvider.GetRequiredService<IParametersDbContext>();
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        var sender = ServiceProvider.GetRequiredService<ISender>();
        var logger = ServiceProvider.GetRequiredService<ILogger<AutomaticDeleteJob>>();
        var client = Factory.HttpClient;

        var settings = new Mock<IOptionsSnapshot<AutomaticDeleteSettings>>();
        settings.Setup(x => x.Value).Returns(new AutomaticDeleteSettings
        {
            DeleteRequestsAutomatically = false,
            DeleteResultsAutomatically = false,
            DeleteQualityAutomatically = true,
            DeleteQualityOlderThanDays = 30
        });

        const string instrument = "MR5487-E2E-SN";
        const string method = "sIgE";
        const string testName = "c1";
        const string devSolutionBarcode = "EK4XXXNZ0001";

        // Create production-like quality and curve-control results through command flow.
        await SeedArticle(parametersDbContext, ArticleType.QualityControlCartridge, "AVM");
        await SeedArticle(parametersDbContext, ArticleType.CurveControlCartridge, "A0V");
        await SendBlankResult(mediator, instrument, devSolutionBarcode);

        var deletedQcName = "QC-E2E-DEL";
        var keptQcName = "QC-E2E-KEEP";
        var deletedCurveControlName = "CC-E2E-DEL";

        await mediator.Send(new ProcessAssayResultCommand(instrument,
            CreateAssayMessage(Guid.NewGuid(), EnumTestType.QualityControl, EnumReagentType.QualityControlCartridge, "AVMXXXSS0001", deletedQcName, method, testName, Guid.NewGuid())), TestContext.Current.CancellationToken);

        await mediator.Send(new ProcessAssayResultCommand(instrument,
            CreateAssayMessage(Guid.NewGuid(), EnumTestType.CurveControl, EnumReagentType.CurveControlCartridge, "A0VXXXSS0001", deletedCurveControlName, method, testName, Guid.NewGuid())), TestContext.Current.CancellationToken);

        var keptRunGuid = Guid.NewGuid();
        await mediator.Send(new ProcessAssayResultCommand(instrument,
            CreateAssayMessage(Guid.NewGuid(), EnumTestType.QualityControl, EnumReagentType.QualityControlCartridge, "AVMXXXSS0001", keptQcName, method, testName, keptRunGuid)), TestContext.Current.CancellationToken);

        var deletedQcId = await dbContext.QualityControlTestResults
            .Where(x => x.QualityControlName == deletedQcName && x.InstrumentNumber == instrument)
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        var deletedCcId = await dbContext.CurveControlTestResults
            .Where(x => x.CurveControlPointName == deletedCurveControlName && x.InstrumentNumber == instrument)
            .OrderByDescending(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync(TestContext.Current.CancellationToken);

        var keptQc = await dbContext.QualityControlTestResults
            .Where(x => x.QualityControlName == keptQcName && x.InstrumentNumber == instrument)
            .OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.AnalyticalRunId, x.QualityControlName, x.TestName })
            .FirstAsync(TestContext.Current.CancellationToken);

        // Mark two rows as quality-history rows not linked to analytical run so cleanup can remove them.
        await dbContext.TestResults
            .Where(x => x.Id == deletedQcId || x.Id == deletedCcId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.AnalyticalRunId, (int?)null), TestContext.Current.CancellationToken);

        keptQc.AnalyticalRunId.Should().NotBeNull();
        var keptRunId = keptQc.AnalyticalRunId!.Value;

        var job = new AutomaticDeleteJob(sender, settings.Object, logger);

        // Act
        await job.Execute(Mock.Of<IJobExecutionContext>());

        // Assert - DB state
        (await dbContext.TestResults.AnyAsync(x => x.Id == deletedQcId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == deletedCcId, TestContext.Current.CancellationToken)).Should().BeFalse();
        (await dbContext.TestResults.AnyAsync(x => x.Id == keptQc.Id, TestContext.Current.CancellationToken)).Should().BeTrue();

        // Assert - API reflects cleanup
        var qcForRunUri = new Uri(Factory.Server.BaseAddress, $"api/results/qc/{keptRunId}");
        var qcForRun = await client.GetFromJsonAsync<List<QualityControlTestResultDto>>(qcForRunUri, TestContext.Current.CancellationToken);

        qcForRun.Should().NotBeNull();
        qcForRun!.Should().ContainSingle(x => x.Id == keptQc.Id);
        qcForRun.Should().NotContain(x => x.Id == deletedQcId);

        var qcQualityUri = new Uri(Factory.Server.BaseAddress,
            $"api/results/qualityControlsForQuality?InstrumentNumber={instrument}&MethodName={method}&QualityControlName={keptQc.QualityControlName}&TestName={keptQc.TestName}&LastDays=5000");
        var qcChart = await client.GetFromJsonAsync<QualityControlChartDto>(qcQualityUri, TestContext.Current.CancellationToken);

        qcChart.Should().NotBeNull();
        qcChart!.Results.Should().NotBeNull();
        qcChart.Results!.Should().ContainSingle(x => x.Id == keptQc.Id);
        qcChart.Results.Should().NotContain(x => x.Id == deletedQcId);
    }

    private static Task SendBlankResult(IMediator mediator, string instrumentNumber, string devSolutionBarcode)
    {
        var blankResult = new BlankResult
        {
            BlankType = EnumBlankType.ReagentImmunoCap,
            ResultOk = true,
            MeanValue = 10,
            UpperLimit = 9999,
            LowerLimit = 0,
            DevelopmentSolutionIdentity = new DevelopmentSolutionIdentity
            {
                ReagentBarcode = devSolutionBarcode,
                CellPosition = 1,
            },
            StopSolutionBarcode = "DoesNotMatter"
        };

        return mediator.Send(new ProcessBlankResultCommand(instrumentNumber, blankResult));
    }

    private static async Task SeedArticle(IParametersDbContext dbContext, ArticleType articleType, string articleNumber)
    {
        var articleExists = await dbContext.Articles.AnyAsync(x => x.ArticleNumber == articleNumber, TestContext.Current.CancellationToken);
        if (!articleExists)
        {
            var article = new Article
            {
                ArticleNumber = articleNumber,
                ArticleType = articleType,
                Quantity = 1,
                MaxDaysInInstrument = 99,
            };
            dbContext.Articles.Add(article);
        }

        var immunocapExists = await dbContext.Articles.AnyAsync(x => x.ArticleNumber == "409", TestContext.Current.CancellationToken);
        if (!immunocapExists)
        {
            var immunocapArticle = new Article
            {
                ArticleNumber = "409",
                ArticleType = ArticleType.ImmunoCapCarrier,
                FullName = "Penicilloyl G",
                Identity = "c1",
                Quantity = 1,
                MaxDaysInInstrument = 99,
            };

            dbContext.Articles.Add(immunocapArticle);
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AssayResult CreateAssayMessage(Guid instrumentReplicateId, EnumTestType testType, EnumReagentType reagentType, string cartridgeBarcode, string sampleId, string methodName, string testName, Guid analyticalRunId)
    {
        var assay = new AssayResult
        {
            TestType = testType,
            TimeOfMeasurement = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2023-09-29 11:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)),
            MethodName = methodName,
            TestName = testName,
            InstrumentDilutionFactor = 1,
            Sample = new SampleInformation
            {
                SampleId = sampleId,
                SampleDate = Timestamp.FromDateTimeOffset(DateTimeOffset.Parse("2023-09-29 11:17:00", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)),
                PreDilutionFactor = 1
            },
            Replicate = new ReplicateInfo
            {
                ReplicateId = instrumentReplicateId.ToString(),
                GroupId = Guid.NewGuid().ToString(),
                ReplicatesInTestOrder = 1,
                TestOrderNumber = 1,
                ReplicateNumber = 1,
                TestOrdersInGroup = 1
            },
            Run = new RunInfo
            {
                AnalyticalRunId = analyticalRunId.ToString(),
                AssayRunId = Guid.NewGuid().ToString(),
            },
            FluorometerResult = new FluorometerValue
            {
                MeasurementValue = 15000
            },
            Reagents =
            {
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.ImmunoCapCarrier,
                    ReagentBarcode = "409XX01",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.DevelopmentSolutionCartridge,
                    ReagentBarcode = "EK4XXXNZ0001",
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991244",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.ConjugateCartridge,
                    ReagentBarcode = "BGRXXX010001",
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991299",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentType = EnumReagentType.StopSolution,
                    ReagentBarcode = "ES1XXX010001",
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991298",
                    CellPosition = 1,
                    ExpiredReagent = false
                },
                new ReagentInformation
                {
                    ReagentBarcode = cartridgeBarcode,
                    ReagentUdiBarcode = "|01510222222333361715070710A213B1|991234",
                    ReagentType = reagentType,
                    CellPosition = 1,
                    ExpiredReagent = false
                }
            }
        };

        return assay;
    }
}
