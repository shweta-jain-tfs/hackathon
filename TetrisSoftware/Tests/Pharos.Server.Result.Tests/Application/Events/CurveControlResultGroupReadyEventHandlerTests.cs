using MediatR;
using Microsoft.Extensions.Logging;
using Moq.EntityFrameworkCore;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Infrastructure;
using Pharos.Server.Result.Application.Commands;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using TestResult = Pharos.Server.Result.Domain.TestResult;

namespace Pharos.Server.Result.Application.Events;

public class CurveControlResultGroupReadyEventHandlerTests
{
    [Fact]
    public async Task Handle_CurveFromCurrentAssay_Flagged_ShouldNotTransitionCalculationState()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;

        var analyticalRun = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            InstrumentRunId = runId,
            CurveSource = CurveSource.CurveFromCurrentAssay,
            CurveState = CurveState.Valid,
            CurveId = 10,
            CurveControlIssue = AnalyticalRunIssue.NoIssue,
            TestResults = new List<TestResult>()
        };

        var curve = new Curve
        {
            Id = 10,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            MeasurementDateTime = created.AddMinutes(-1),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = runId
        };

        var ccResult = new CurveControlTestResult
        {
            Id = 100,
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            TestName = "Test1",
            CurveControlPointName = "CC1",
            GroupId = groupId,
            Created = created,
            Replicates = new List<TestReplicate>
            {
                new CurveControlTestReplicate
                {
                    Id = 200,
                    InstrumentReplicateId = Guid.NewGuid(),
                    Active = true,
                    IsAborted = false,
                    UsedBlankId = 1,
                    Concentration = 15,
                    ConcentrationStatus = ConcentrationStatus.Ok,
                    MeasurementDateTime = created
                },
                new CurveControlTestReplicate
                {
                    Id = 201,
                    InstrumentReplicateId = Guid.NewGuid(),
                    Active = true,
                    IsAborted = false,
                    UsedBlankId = 1,
                    Concentration = 5,
                    ConcentrationStatus = ConcentrationStatus.Ok,
                    MeasurementDateTime = created
                }
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet(new List<CurveControlTestResult> { ccResult });
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun> { analyticalRun });
        dbMock.Setup(db => db.Curves).ReturnsDbSet(new List<Curve> { curve });
        dbMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet(new List<TestReplicateError>());
        dbMock.Setup(db => db.TestReplicates).ReturnsDbSet(new List<TestReplicate>());
        dbMock.Setup(db => db.TestReplicateReagents).ReturnsDbSet(new List<TestReplicateReagent>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, bool>>>()))
            .ReturnsAsync(false);

        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, object>>>()))
            .ReturnsAsync(null as object);

        var resultCalculationServiceMock = new Mock<IResultCalculationService>();
        resultCalculationServiceMock
            .Setup(s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        resultCalculationServiceMock
            .Setup(s => s.CalculateSummaryResult(It.IsAny<TestResult>()))
            .Returns(Task.CompletedTask);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurveControlJudgmentResult.Flagged);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(created);
        var loggerMock = new Mock<ILogger<CurveControlResultGroupReadyEventHandler>>();

        var sut = new CurveControlResultGroupReadyEventHandler(
            dbMock.Object,
            methodsServiceMock.Object,
            resultCalculationServiceMock.Object,
            mediatorMock.Object,
            dateTimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.Handle(new CurveControlResultGroupReadyEvent("SN001", groupId, "Method1"), TestContext.Current.CancellationToken);

        // Assert
        analyticalRun.CurveState.Should().Be(CurveState.Valid);

        mediatorMock.Verify(
            m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);

        dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        analyticalRun.DomainEvents.OfType<AnalyticalRunCalculationCurveUpdatedByCurveControl>()
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData(CurveControlJudgmentResult.NotOK)]
    [InlineData(CurveControlJudgmentResult.ReRunNotOK)]
    public async Task Handle_FailedCurveControl_ShouldExpireCurveUsageToPreviousDay(CurveControlJudgmentResult judgmentResult)
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 01, 10, 11, 0, 0, TimeSpan.Zero);

        var curve = new Curve
        {
            Id = 10,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            MeasurementDateTime = now.AddHours(-1),
            LastDayToUse = new DateOnly(2026, 12, 31),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = runId
        };

        var analyticalRun = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            InstrumentRunId = runId,
            CurveSource = CurveSource.CurveFromPreviousAssay,
            CurveState = CurveState.Valid,
            CurveId = curve.Id,
            CalculationCurve = curve,
            TestResults = new List<TestResult>()
        };

        var ccResult = new CurveControlTestResult
        {
            Id = 100,
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            TestName = "Test1",
            CurveControlPointName = "CC1",
            GroupId = groupId,
            Created = now,
            Replicates = new List<TestReplicate>
            {
                new CurveControlTestReplicate
                {
                    Id = 200,
                    InstrumentReplicateId = Guid.NewGuid(),
                    Active = true,
                    IsAborted = false,
                    UsedBlankId = 1,
                    Concentration = 15,
                    ConcentrationStatus = ConcentrationStatus.Ok,
                    MeasurementDateTime = now
                }
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet(new List<CurveControlTestResult> { ccResult });
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun> { analyticalRun });
        dbMock.Setup(db => db.Curves).ReturnsDbSet(new List<Curve> { curve });
        dbMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet(new List<TestReplicateError>());
        dbMock.Setup(db => db.TestReplicates).ReturnsDbSet(new List<TestReplicate>());
        dbMock.Setup(db => db.TestReplicateReagents).ReturnsDbSet(new List<TestReplicateReagent>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, bool>>>()))
            .ReturnsAsync(false);

        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, object>>>()))
            .ReturnsAsync(null as object);

        var resultCalculationServiceMock = new Mock<IResultCalculationService>();
        resultCalculationServiceMock
            .Setup(s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        resultCalculationServiceMock
            .Setup(s => s.CalculateSummaryResult(It.IsAny<TestResult>()))
            .Returns(Task.CompletedTask);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(judgmentResult);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(now);
        var loggerMock = new Mock<ILogger<CurveControlResultGroupReadyEventHandler>>();

        var sut = new CurveControlResultGroupReadyEventHandler(
            dbMock.Object,
            methodsServiceMock.Object,
            resultCalculationServiceMock.Object,
            mediatorMock.Object,
            dateTimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.Handle(new CurveControlResultGroupReadyEvent("SN001", groupId, "Method1"), TestContext.Current.CancellationToken);

        // Assert
        analyticalRun.CurveState.Should().Be(CurveState.Invalid);
        analyticalRun.CurveSource.Should().Be(CurveSource.Missing);
        curve.LastDayToUse.Should().Be(DateOnly.FromDateTime(now.Date.AddDays(-1)));
    }

    [Fact]
    public async Task Handle_FlaggedCurveControl_ShouldExpireCurveUsageToPreviousDay()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 01, 10, 11, 0, 0, TimeSpan.Zero);

        var curve = new Curve
        {
            Id = 10,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            MeasurementDateTime = now.AddHours(-1),
            LastDayToUse = new DateOnly(2026, 12, 31),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = runId
        };

        var analyticalRun = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            InstrumentRunId = runId,
            CurveSource = CurveSource.CurveFromPreviousAssay,
            CurveState = CurveState.Valid,
            CurveId = curve.Id,
            CalculationCurve = curve,
            TestResults = new List<TestResult>()
        };

        var ccResult = new CurveControlTestResult
        {
            Id = 100,
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            TestName = "Test1",
            CurveControlPointName = "CC1",
            GroupId = groupId,
            Created = now,
            Replicates = new List<TestReplicate>
            {
                new CurveControlTestReplicate
                {
                    Id = 200,
                    InstrumentReplicateId = Guid.NewGuid(),
                    Active = true,
                    IsAborted = false,
                    UsedBlankId = 1,
                    Concentration = 15,
                    ConcentrationStatus = ConcentrationStatus.Ok,
                    MeasurementDateTime = now
                }
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet(new List<CurveControlTestResult> { ccResult });
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun> { analyticalRun });
        dbMock.Setup(db => db.Curves).ReturnsDbSet(new List<Curve> { curve });
        dbMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet(new List<TestReplicateError>());
        dbMock.Setup(db => db.TestReplicates).ReturnsDbSet(new List<TestReplicate>());
        dbMock.Setup(db => db.TestReplicateReagents).ReturnsDbSet(new List<TestReplicateReagent>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, bool>>>()))
            .ReturnsAsync(false);

        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, object>>>()))
            .ReturnsAsync(null as object);

        var resultCalculationServiceMock = new Mock<IResultCalculationService>();
        resultCalculationServiceMock
            .Setup(s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        resultCalculationServiceMock
            .Setup(s => s.CalculateSummaryResult(It.IsAny<TestResult>()))
            .Returns(Task.CompletedTask);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurveControlJudgmentResult.Flagged);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(now);
        var loggerMock = new Mock<ILogger<CurveControlResultGroupReadyEventHandler>>();

        var sut = new CurveControlResultGroupReadyEventHandler(
            dbMock.Object,
            methodsServiceMock.Object,
            resultCalculationServiceMock.Object,
            mediatorMock.Object,
            dateTimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.Handle(new CurveControlResultGroupReadyEvent("SN001", groupId, "Method1"), TestContext.Current.CancellationToken);

        // Assert
        analyticalRun.CurveState.Should().Be(CurveState.Invalid);
        analyticalRun.CurveSource.Should().Be(CurveSource.CurveFromPreviousAssay);
        curve.LastDayToUse.Should().Be(DateOnly.FromDateTime(now.Date.AddDays(-1)));
    }

    [Fact]
    public async Task Handle_WithCurveSourceMissing_ShouldSkipConcentrationCalculation()
    {
        // Arrange: Test the new CurveId.HasValue defensive guard
        // When CurveSource is Missing, concentrations should NOT be calculated
        // even if other conditions might seem favorable
        var groupId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;

        var analyticalRun = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            InstrumentRunId = runId,
            CurveSource = CurveSource.Missing,  // ← No curve assigned
            CurveState = CurveState.Valid,  // Even if Valid state, guard should block
            CurveId = null,  // ← CurveId is null (invariant guard checks this)
            TestResults = new List<TestResult>()
        };

        var ccResult = new CurveControlTestResult
        {
            Id = 100,
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            TestName = "Test1",
            CurveControlPointName = "CC1",
            GroupId = groupId,
            Created = created,
            Replicates = new List<TestReplicate>
            {
                new CurveControlTestReplicate
                {
                    Id = 200,
                    InstrumentReplicateId = Guid.NewGuid(),
                    Active = true,
                    IsAborted = false,
                    UsedBlankId = 1,
                    Concentration = null,
                    ConcentrationStatus = ConcentrationStatus.Ok,
                    MeasurementDateTime = created
                }
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet(new List<CurveControlTestResult> { ccResult });
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun> { analyticalRun });
        dbMock.Setup(db => db.Curves).ReturnsDbSet(new List<Curve>());
        dbMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet(new List<TestReplicateError>());
        dbMock.Setup(db => db.TestReplicates).ReturnsDbSet(new List<TestReplicate>());
        dbMock.Setup(db => db.TestReplicateReagents).ReturnsDbSet(new List<TestReplicateReagent>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, object>>>()))
            .ReturnsAsync(null as object);

        var resultCalculationServiceMock = new Mock<IResultCalculationService>();
        // Should NOT be called for concentration calculation when gate fails
        resultCalculationServiceMock
            .Setup(s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurveControlJudgmentResult.Flagged);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(created);
        var loggerMock = new Mock<ILogger<CurveControlResultGroupReadyEventHandler>>();

        var sut = new CurveControlResultGroupReadyEventHandler(
            dbMock.Object,
            methodsServiceMock.Object,
            resultCalculationServiceMock.Object,
            mediatorMock.Object,
            dateTimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.Handle(new CurveControlResultGroupReadyEvent("SN001", groupId, "Method1"), TestContext.Current.CancellationToken);

        // Assert
        // The concentration calculation should NOT have been called (gate blocked it)
        resultCalculationServiceMock.Verify(
            s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "CalculateReplicateConcentration should not be called when CurveSource is Missing");

        // Database should still be saved (even if concentrations weren't calculated)
        dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCurveAssigned_ShouldCalculateConcentrations()
    {
        // Arrange: Verify the guard ALLOWS calculation when invariant is satisfied
        // When CurveId has a value and CurveSource is not Missing, proceed with calculation
        var groupId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;

        var analyticalRun = new AnalyticalRun
        {
            Id = 1,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            InstrumentRunId = runId,
            CurveSource = CurveSource.CurveFromCurrentAssay,  // ← Curve assigned
            CurveState = CurveState.Valid,  // ← Valid state
            CurveId = 10,  // ← CurveId is set (invariant satisfied)
            TestResults = new List<TestResult>()
        };

        var curve = new Curve
        {
            Id = 10,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            MeasurementDateTime = created.AddMinutes(-1),
            GroupId = Guid.NewGuid(),
            InstrumentRunId = runId
        };

        var ccResult = new CurveControlTestResult
        {
            Id = 100,
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            InstrumentNumber = "SN001",
            MethodName = "Method1",
            TestName = "Test1",
            CurveControlPointName = "CC1",
            GroupId = groupId,
            Created = created,
            Replicates = new List<TestReplicate>
            {
                new CurveControlTestReplicate
                {
                    Id = 200,
                    InstrumentReplicateId = Guid.NewGuid(),
                    Active = true,
                    IsAborted = false,
                    UsedBlankId = 1,
                    Concentration = null,
                    ConcentrationStatus = ConcentrationStatus.Ok,
                    MeasurementDateTime = created
                }
            }
        };

        var dbMock = new Mock<IResultDbContext>();
        dbMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet(new List<CurveControlTestResult> { ccResult });
        dbMock.Setup(db => db.AnalyticalRuns).ReturnsDbSet(new List<AnalyticalRun> { analyticalRun });
        dbMock.Setup(db => db.Curves).ReturnsDbSet(new List<Curve> { curve });
        dbMock.Setup(db => db.TestReplicateErrors).ReturnsDbSet(new List<TestReplicateError>());
        dbMock.Setup(db => db.TestReplicates).ReturnsDbSet(new List<TestReplicate>());
        dbMock.Setup(db => db.TestReplicateReagents).ReturnsDbSet(new List<TestReplicateReagent>());
        dbMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var methodsServiceMock = new Mock<IMethodsService>();
        methodsServiceMock
            .Setup(m => m.GetMethod("Method1", It.IsAny<System.Linq.Expressions.Expression<Func<Method, object>>>()))
            .ReturnsAsync(null as object);

        var resultCalculationServiceMock = new Mock<IResultCalculationService>();
        // Should be called when guard passes
        resultCalculationServiceMock
            .Setup(s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CurveControlJudgmentResult.Flagged);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(created);
        var loggerMock = new Mock<ILogger<CurveControlResultGroupReadyEventHandler>>();

        var sut = new CurveControlResultGroupReadyEventHandler(
            dbMock.Object,
            methodsServiceMock.Object,
            resultCalculationServiceMock.Object,
            mediatorMock.Object,
            dateTimeMock.Object,
            loggerMock.Object);

        // Act
        await sut.Handle(new CurveControlResultGroupReadyEvent("SN001", groupId, "Method1"), TestContext.Current.CancellationToken);

        // Assert
        // Concentration calculation SHOULD have been called (gate allowed it)
        resultCalculationServiceMock.Verify(
            s => s.CalculateReplicateConcentration(
                It.IsAny<TestReplicate>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TestResultType>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "CalculateReplicateConcentration should be called when CurveId is set and CurveSource is not Missing");

        // Curve controls should also be evaluated
        mediatorMock.Verify(
            m => m.Send(It.IsAny<EvaluateCurveControlsCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Database should be saved
        dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2183")]
    [Trait("ModuleRequirement", "MR_6056")]
    [Fact]
    public async Task Handle_ShouldSendCurveControlResultToLabCommunity()
    {
        const string instrumentNumber = "SN001";
        const string methodName = "Method1";
        var groupId = Guid.NewGuid();
        var measurementTime = new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
        var replicate = new CurveControlTestReplicate
        {
            Id = 200,
            InstrumentReplicateId = Guid.NewGuid(),
            ReplicateNumber = 1,
            MeasurementDateTime = measurementTime,
            Concentration = 12.8,
            Response = 100.2,
            Judgment = CurveControlJudgment.OK,
            TestReplicateReagents =
            [
                CreateReagent(200, ArticleType.ConjugateCartridge, "CON1"),
                CreateReagent(200, ArticleType.EliaWellCarrier, "SOL1"),
                CreateReagent(200, ArticleType.CurveControlCartridge, "CCL1")
            ]
        };
        var result = new CurveControlTestResult
        {
            GroupId = groupId,
            InstrumentNumber = instrumentNumber,
            MeasurementDateTime = measurementTime,
            CurveControlPointName = "CC1",
            MethodName = methodName,
            TestName = "Test1",
            JudgmentSummary = CurveControlJudgment.FlaggedHigh,
            ExpectedConcentration = 12.5,
            Replicates = [replicate]
        };
        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock.Setup(db => db.CurveControlTestResults).ReturnsDbSet([result]);
        dbContextMock.Setup(db => db.TestReplicates).ReturnsDbSet([replicate]);
        dbContextMock.Setup(db => db.TestReplicateReagents).ReturnsDbSet(replicate.TestReplicateReagents!);

        SendCurveControlResultToLabCommunityCommand? sentCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(mediator => mediator.Send(It.IsAny<SendCurveControlResultToLabCommunityCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SendCurveControlResultToLabCommunityCommand, CancellationToken>((command, _) => sentCommand = command);
        var sut = new CurveControlResultGroupReadyEventHandler(
            dbContextMock.Object,
            Mock.Of<IMethodsService>(),
            Mock.Of<IResultCalculationService>(),
            mediatorMock.Object,
            Mock.Of<IDateTime>(),
            Mock.Of<ILogger<CurveControlResultGroupReadyEventHandler>>());

        await sut.Handle(
            new CurveControlResultGroupReadyEvent(instrumentNumber, groupId, methodName),
            TestContext.Current.CancellationToken);

        sentCommand.Should().BeEquivalentTo(new SendCurveControlResultToLabCommunityCommand(
            groupId,
            instrumentNumber,
            measurementTime,
            "CC1",
            methodName,
            "Test1",
            CurveControlResultJudgment.FlaggedHigh,
            12.5,
            "CON1",
            "SOL1",
            "CCL1",
            [new CurveControlResultReplicate(1, measurementTime, 12.8, 100.2, CurveControlResultJudgment.Ok, "CON1", "SOL1", "CCL1")]));
    }

    private static TestReplicateReagent CreateReagent(int replicateId, ArticleType type, string lotNumber)
    {
        return new TestReplicateReagent
        {
            TestReplicateId = replicateId,
            ReagentType = type,
            LotNumber = lotNumber
        };
    }
}
