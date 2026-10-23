using MediatR;
using Moq.EntityFrameworkCore;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Events.TestResultReady;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;
using TestResult = Pharos.Server.Result.Domain.TestResult;

namespace Pharos.Server.Result.Application.Events;

public class SampleTestResultReadyEventHandlerTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_4107")]
    public async Task Handle_WhenResultIsDoubtful_ShouldSetStatusToFlagged()
    {
        var testResult = CreateTestResult(new SampleTestReplicate
        {
            Id = 1,
            Active = true,
            UsedBlankId = 1
        });
        testResult.MeanConcentrationStatus = ConcentrationStatus.Doubtful;

        var sut = CreateSut(testResult, []);

        await sut.Handle(
            new SampleTestResultReadyEvent(testResult),
            TestContext.Current.CancellationToken);

        testResult.Status.Should().Be(TestResultStatus.Flagged);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_4107")]
    public async Task Handle_WhenResultIsDoubtful_ShouldRaiseTestIssue()
    {
        var analyticalRun = new AnalyticalRun { Id = 42 };
        var testResult = CreateTestResult(new SampleTestReplicate
        {
            Id = 1,
            Active = true,
            UsedBlankId = 1
        });
        testResult.AnalyticalRunId = analyticalRun.Id;
        testResult.MeanConcentrationStatus = ConcentrationStatus.Doubtful;

        var sut = CreateSut(testResult, [], [analyticalRun]);

        await sut.Handle(
            new SampleTestResultReadyEvent(testResult),
            TestContext.Current.CancellationToken);

        analyticalRun.DomainEvents
            .OfType<UpdateAnalyticalRunQualityIssue>()
            .Should().ContainSingle(issue => issue.TestIssue == AnalyticalRunIssue.Warning);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_4107")]
    public async Task Handle_WhenReplicateIsAborted_ShouldSetStatusToAborted()
    {
        var testResult = CreateTestResult(new SampleTestReplicate
        {
            Id = 1,
            Active = true,
            IsAborted = true
        });

        var sut = CreateSut(testResult, []);

        await sut.Handle(
            new SampleTestResultReadyEvent(testResult),
            TestContext.Current.CancellationToken);

        testResult.Status.Should().Be(TestResultStatus.Aborted);
    }

    private static SampleTestResultReadyEventHandler CreateSut(
        SampleTestResult testResult,
        List<TestReplicateError> replicateErrors,
        List<AnalyticalRun>? analyticalRuns = null)
    {
        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock
            .Setup(dbContext => dbContext.TestResults)
            .ReturnsDbSet(new List<TestResult> { testResult });
        dbContextMock
            .Setup(dbContext => dbContext.TestReplicateErrors)
            .ReturnsDbSet(replicateErrors);
        dbContextMock
            .Setup(dbContext => dbContext.AnalyticalRuns)
            .ReturnsDbSet(analyticalRuns ?? []);
        dbContextMock
            .Setup(dbContext => dbContext.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var resultCalculationServiceMock = new Mock<IResultCalculationService>();
        resultCalculationServiceMock
            .Setup(service => service.CalculateSummaryResult(testResult))
            .Returns(Task.CompletedTask);

        return new SampleTestResultReadyEventHandler(
            dbContextMock.Object,
            resultCalculationServiceMock.Object,
            new Mock<IMediator>().Object);
    }

    private static SampleTestResult CreateTestResult(SampleTestReplicate replicate)
    {
        return new SampleTestResult
        {
            NumberOfReplicates = 1,
            Replicates = new List<TestReplicate> { replicate }
        };
    }
}
