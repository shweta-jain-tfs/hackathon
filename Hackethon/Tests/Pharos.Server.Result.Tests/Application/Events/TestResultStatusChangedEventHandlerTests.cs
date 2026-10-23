using Moq.EntityFrameworkCore;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Result.Application.Events;

public class TestResultStatusChangedEventHandlerTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_4108")]
    public async Task Handle_WhenAnotherDoubtfulResultRemains_ShouldNotClearTestIssue()
    {
        var analyticalRun = new AnalyticalRun
        {
            Id = 42,
            TestIssue = AnalyticalRunIssue.Warning
        };
        var approvedResult = new SampleTestResult
        {
            Id = 1,
            TestOrderId = Guid.NewGuid(),
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            ApprovalStatus = ResultApprovalStatus.Approved,
            Replicates = []
        };
        var remainingDoubtfulResult = new SampleTestResult
        {
            Id = 2,
            TestOrderId = Guid.NewGuid(),
            AnalyticalRunId = analyticalRun.Id,
            AnalyticalRun = analyticalRun,
            ApprovalStatus = ResultApprovalStatus.NotApproved,
            MeanConcentrationStatus = ConcentrationStatus.Doubtful,
            Status = TestResultStatus.Flagged,
            Replicates = []
        };
        var sampleTestResults = new List<SampleTestResult> { approvedResult, remainingDoubtfulResult };
        var dbContextMock = new Mock<IResultDbContext>();
        dbContextMock.Setup(dbContext => dbContext.SampleTestResults).ReturnsDbSet(sampleTestResults);
        dbContextMock.Setup(dbContext => dbContext.AnalyticalRuns).ReturnsDbSet([analyticalRun]);
        dbContextMock
            .Setup(dbContext => dbContext.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var sut = new TestResultStatusChangedEventHandler(dbContextMock.Object);

        await sut.Handle(
            new TestResultStatusChangedEvent([approvedResult]),
            TestContext.Current.CancellationToken);

        analyticalRun.DomainEvents
            .OfType<UpdateAnalyticalRunQualityIssue>()
            .Should().BeEmpty("the unresolved doubtful result still requires a ResultsIssue task card");
    }
}
