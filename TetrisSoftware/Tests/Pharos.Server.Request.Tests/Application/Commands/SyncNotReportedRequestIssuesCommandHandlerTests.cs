using Microsoft.Extensions.Options;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Request.Application.Commands.RequestIssue;
using Pharos.Server.Domain.Parameters.Methods;
using System.Linq.Expressions;

namespace Pharos.Server.Request.Application.Commands;

public class SyncNotReportedRequestIssuesCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock = new();
    private readonly Mock<IMethodsService> _methodsServiceMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IRequestHub> _hubMock = new();

    [Fact]
    public async Task Handle_ShouldCreateNotReportedIssue_ForCompletedRequest_WhenMethodDefaultChannelHasAutomaticExportEnabled()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(
            channel0Trigger: LisAutoExportTrigger.NotUsed,
            channel2Trigger: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected));
        SetupDefaultLisChannels(("MethodWithDefaultChannel", 2));

        var request = CreateRequest(
            requestId: 42,
            requestState: RequestState.Completed,
            testOrders:
            [
                CreateTestOrder(TestOrderState.Approved, lisChannelId: null, methodName: "MethodWithDefaultChannel"),
                CreateTestOrder(TestOrderState.Reported, lisChannelId: null, methodName: "MethodWithDefaultChannel")
            ]);

        SetupDatabase([request], []);

        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);

        var sut = new SyncNotReportedRequestIssuesCommandHandler(
            _dbContextMock.Object,
            evaluator,
            _senderMock.Object,
            _hubMock.Object);

        // Act
        await sut.Handle(new SyncNotReportedRequestIssuesCommand(), TestContext.Current.CancellationToken);

        // Assert
        Mock.Get(_dbContextMock.Object.Issues).Verify(
            x => x.AddRange(It.Is<IEnumerable<Domain.RequestIssue>>(issues =>
                issues.Single().RequestId == 42
                && issues.Single().IssueType == RequestIssueType.NotReported)),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hubMock.Verify(x => x.Updated(42), Times.Once);
        VerifyTaskCardsRegeneratedOnce();
    }

    [Fact]
    public async Task Handle_ShouldOnlyDeleteStaleIssues()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(
            channel0Trigger: LisAutoExportTrigger.WhenTestIsApprovedOrRejected));

        var matchingRequest = CreateRequest(
            requestId: 43,
            requestState: RequestState.Completed,
            testOrders: [CreateTestOrder(TestOrderState.Approved, lisChannelId: 0, methodName: "Method")]);
        var unrelatedRequest = CreateRequest(
            requestId: 44,
            requestState: RequestState.Started,
            testOrders: [CreateTestOrder(TestOrderState.Started, lisChannelId: 0, methodName: "Method")]);

        SetupDatabase(
            [matchingRequest, unrelatedRequest],
            [
                new Domain.RequestIssue { RequestId = matchingRequest.Id, IssueType = RequestIssueType.NotReported },
                new Domain.RequestIssue { RequestId = 45, IssueType = RequestIssueType.NotReported }
            ]);

        var sut = CreateHandler();

        // Act
        await sut.Handle(new SyncNotReportedRequestIssuesCommand(), TestContext.Current.CancellationToken);

        // Assert
        Mock.Get(_dbContextMock.Object.Issues).Verify(
            x => x.RemoveRange(It.Is<IEnumerable<Domain.RequestIssue>>(issues =>
                issues.Single().RequestId == 45)),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hubMock.Verify(x => x.Updated(45), Times.Once);
        VerifyTaskCardsRegeneratedOnce();
    }

    /// <summary>
    /// Verifies that synchronization removes an existing Report results issue when automatic export is disabled.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldDeleteExistingNotReportedIssue_WhenAutomaticExportIsDisabled()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(LisAutoExportTrigger.NotUsed));

        var request = CreateRequest(
            requestId: 46,
            requestState: RequestState.Completed,
            testOrders: [CreateTestOrder(TestOrderState.Approved, lisChannelId: 0, methodName: "Method")]);

        SetupDatabase(
            [request],
            [new Domain.RequestIssue { RequestId = request.Id, IssueType = RequestIssueType.NotReported }]);

        var sut = CreateHandler();

        // Act
        await sut.Handle(new SyncNotReportedRequestIssuesCommand(), TestContext.Current.CancellationToken);

        // Assert
        Mock.Get(_dbContextMock.Object.Issues).Verify(
            x => x.RemoveRange(It.Is<IEnumerable<Domain.RequestIssue>>(issues =>
                issues.Single().RequestId == request.Id)),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hubMock.Verify(x => x.Updated(request.Id), Times.Once);
        VerifyTaskCardsRegeneratedOnce();
    }

    [Fact]
    public async Task Handle_ShouldSaveAndRegenerateTaskCardsOnce_WhenSeveralIssuesChange()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(
            LisAutoExportTrigger.WhenTestIsApprovedOrRejected));

        var firstRequest = CreateRequest(
            requestId: 47,
            requestState: RequestState.Completed,
            testOrders: [CreateTestOrder(TestOrderState.Approved, lisChannelId: 0, methodName: "Method")]);
        var secondRequest = CreateRequest(
            requestId: 48,
            requestState: RequestState.Completed,
            testOrders: [CreateTestOrder(TestOrderState.Rejected, lisChannelId: 0, methodName: "Method")]);

        SetupDatabase([firstRequest, secondRequest], []);
        var sut = CreateHandler();

        // Act
        await sut.Handle(new SyncNotReportedRequestIssuesCommand(), TestContext.Current.CancellationToken);

        // Assert
        Mock.Get(_dbContextMock.Object.Issues).Verify(
            x => x.AddRange(It.Is<IEnumerable<Domain.RequestIssue>>(issues => issues.Count() == 2)),
            Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hubMock.Verify(x => x.Updated(firstRequest.Id), Times.Once);
        _hubMock.Verify(x => x.Updated(secondRequest.Id), Times.Once);
        VerifyTaskCardsRegeneratedOnce();
    }

    private SyncNotReportedRequestIssuesCommandHandler CreateHandler()
    {
        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);
        return new SyncNotReportedRequestIssuesCommandHandler(
            _dbContextMock.Object,
            evaluator,
            _senderMock.Object,
            _hubMock.Object);
    }

    private void VerifyTaskCardsRegeneratedOnce()
    {
        _senderMock.Verify(
            x => x.Send(It.IsAny<RegenerateRequestIssueTaskCardsCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _senderMock.Verify(
            x => x.Send(It.IsAny<CreateRequestIssueCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _senderMock.Verify(
            x => x.Send(It.IsAny<DeleteRequestIssueCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupDatabase(
        IReadOnlyList<Domain.Request> requests,
        IReadOnlyList<Domain.RequestIssue> issues)
    {
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
        _dbContextMock.Setup(x => x.Issues).ReturnsDbSet(issues);
    }

    private void SetupDefaultLisChannels(params (string MethodName, int? LisChannelId)[] methods)
    {
        _methodsServiceMock
            .Setup(x => x.GetMethods(
                It.IsAny<Expression<Func<Method, bool>>>(),
                It.IsAny<Expression<Func<Method, MethodDefaultLisChannel>>>()))
            .ReturnsAsync(methods
                .Select(method => new MethodDefaultLisChannel(method.MethodName, method.LisChannelId))
                .ToList());
    }

    private static Domain.Request CreateRequest(int requestId, RequestState requestState, IReadOnlyList<TestOrder> testOrders)
    {
        var request = new Domain.Request
        {
            Id = requestId,
            SampleId = $"SAMPLE-{requestId}",
            Created = DateTimeOffset.UtcNow,
            RequestState = requestState,
            Source = RequestSource.Lis
        };

        foreach (var testOrder in testOrders)
        {
            testOrder.RequestId = requestId;
            testOrder.Request = request;
            request.TestOrders.Add(testOrder);
        }

        return request;
    }

    private static TestOrder CreateTestOrder(TestOrderState state, int? lisChannelId, string methodName)
    {
        return new TestOrder
        {
            Uid = Guid.NewGuid(),
            MethodName = methodName,
            TestName = "Test",
            IsActive = true,
            State = state,
            LisChannelId = lisChannelId
        };
    }

    private static LisSettings CreateLisSettings(
        LisAutoExportTrigger channel0Trigger,
        LisAutoExportTrigger? channel2Trigger = null)
    {
        var lisChannels = new List<LisChannel>
        {
            new()
            {
                Name = "LIS-1",
                ExportSettings = new LisExportSettings
                {
                    Sample = new LisExportSampleSettings
                    {
                        AutomaticSampleExport = channel0Trigger
                    }
                }
            }
        };

        if (channel2Trigger.HasValue)
        {
            lisChannels.Add(new LisChannel { Name = "LIS-2" });
            lisChannels.Add(new LisChannel
            {
                Name = "LIS-3",
                ExportSettings = new LisExportSettings
                {
                    Sample = new LisExportSampleSettings
                    {
                        AutomaticSampleExport = channel2Trigger.Value
                    }
                }
            });
        }

        return new LisSettings
        {
            LisChannels = lisChannels
        };
    }
}
