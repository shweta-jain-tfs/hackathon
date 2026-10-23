using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Request.Application.Commands.RequestIssue;
using System.Linq.Expressions;

namespace Pharos.Server.Request.Application.Commands;

public class RequestIssueEventHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IInstrumentManager> _instrumentManagerMock = new();
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock = new();
    private readonly Mock<IMethodsService> _methodsServiceMock = new();

    [Fact]
    public async Task Handle_ShouldCreateNotReportedIssue_WhenAnyTestIsApprovedOrRejected()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(LisAutoExportTrigger.WhenSampleIsApprovedOrRejected));

        var request = CreateRequest(
            requestId: 44,
            requestState: RequestState.Completed,
            testOrders:
            [
                CreateTestOrder(TestOrderState.Reported, lisChannelId: 0),
                CreateTestOrder(TestOrderState.Approved, lisChannelId: 0)
            ]);

        SetupRequests([request]);
        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);
        var sut = new RequestIssueEventHandler(_dbContextMock.Object, _mediatorMock.Object, _instrumentManagerMock.Object, evaluator);

        // Act
        await sut.Handle(CreateRequestUpdatedEvent(request), TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<CreateRequestIssueCommand>(command =>
                    command.RequestId == 44
                    && command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            x => x.Send(
                It.Is<DeleteRequestIssueCommand>(command => command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateNotReportedIssue_WhenMethodDefaultChannelHasAutomaticExportEnabled()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(
            channel0Trigger: LisAutoExportTrigger.NotUsed,
            channel2Trigger: LisAutoExportTrigger.WhenSampleIsApprovedOrRejected));
        SetupDefaultLisChannels(("MethodWithDefaultChannel", 2));

        var request = CreateRequest(
            requestId: 45,
            requestState: RequestState.Completed,
            testOrders:
            [
                CreateTestOrder(TestOrderState.Approved, lisChannelId: null, methodName: "MethodWithDefaultChannel"),
                CreateTestOrder(TestOrderState.Rejected, lisChannelId: null, methodName: "MethodWithDefaultChannel")
            ]);

        SetupRequests([request]);
        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);
        var sut = new RequestIssueEventHandler(_dbContextMock.Object, _mediatorMock.Object, _instrumentManagerMock.Object, evaluator);

        // Act
        await sut.Handle(CreateRequestUpdatedEvent(request), TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<CreateRequestIssueCommand>(command =>
                    command.RequestId == 45
                    && command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<TaskCardCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(TestOrderState.Rejected, WhatToExport.OnlyApprovedResults, RequestSource.Lis)]
    [InlineData(TestOrderState.Approved, WhatToExport.AllStatuses, RequestSource.Manual)]
    public async Task Handle_ShouldCreateNotReportedIssue_WhenResultIsExcludedFromAutomaticExportFilters(
        TestOrderState testOrderState,
        WhatToExport whatToExport,
        RequestSource requestSource)
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(
            LisAutoExportTrigger.WhenTestIsApprovedOrRejected,
            whatToExport: whatToExport));

        var request = CreateRequest(
            requestId: 46,
            requestState: RequestState.Completed,
            testOrders: [CreateTestOrder(testOrderState, lisChannelId: 0)],
            source: requestSource);

        SetupRequests([request]);
        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);
        var sut = new RequestIssueEventHandler(_dbContextMock.Object, _mediatorMock.Object, _instrumentManagerMock.Object, evaluator);

        // Act
        await sut.Handle(CreateRequestUpdatedEvent(request), TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<CreateRequestIssueCommand>(command =>
                    command.RequestId == request.Id
                    && command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            x => x.Send(
                It.Is<DeleteRequestIssueCommand>(command => command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that Report results is not raised before the request reaches Completed or when automatic export is disabled.
    /// </summary>
    [Theory]
    [InlineData(RequestState.Started, LisAutoExportTrigger.WhenTestIsApprovedOrRejected)]
    [InlineData(RequestState.Completed, LisAutoExportTrigger.NotUsed)]
    public async Task Handle_ShouldRemoveNotReportedIssue_WhenRequestIsNotEligible(
        RequestState requestState,
        LisAutoExportTrigger automaticExport)
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(automaticExport));

        var request = CreateRequest(
            requestId: 47,
            requestState: requestState,
            testOrders: [CreateTestOrder(TestOrderState.Approved, lisChannelId: 0)]);

        SetupRequests([request]);
        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);
        var sut = new RequestIssueEventHandler(_dbContextMock.Object, _mediatorMock.Object, _instrumentManagerMock.Object, evaluator);

        // Act
        await sut.Handle(CreateRequestUpdatedEvent(request), TestContext.Current.CancellationToken);

        // Assert
        VerifyNotReportedIssueRemoved(request.Id);
    }

    /// <summary>
    /// Verifies that reporting every pending result clears the Report results issue.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldRemoveNotReportedIssue_WhenAllResultsAreReported()
    {
        // Arrange
        _lisSettingsMock.Setup(x => x.Value).Returns(CreateLisSettings(LisAutoExportTrigger.WhenTestIsApprovedOrRejected));

        var request = CreateRequest(
            requestId: 48,
            requestState: RequestState.Completed,
            testOrders:
            [
                CreateTestOrder(TestOrderState.Reported, lisChannelId: 0),
                CreateTestOrder(TestOrderState.Reported, lisChannelId: 0)
            ]);

        SetupRequests([request]);
        var evaluator = new NotReportedRequestIssueEvaluator(_lisSettingsMock.Object, _methodsServiceMock.Object);
        var sut = new RequestIssueEventHandler(_dbContextMock.Object, _mediatorMock.Object, _instrumentManagerMock.Object, evaluator);

        // Act
        await sut.Handle(CreateRequestUpdatedEvent(request), TestContext.Current.CancellationToken);

        // Assert
        VerifyNotReportedIssueRemoved(request.Id);
    }

    private void SetupRequests(IReadOnlyList<Domain.Request> requests)
    {
        _dbContextMock
            .Setup(x => x.Requests)
            .ReturnsDbSet(requests);
    }

    private static RequestUpdatedEvent CreateRequestUpdatedEvent(Domain.Request request)
    {
        return new RequestUpdatedEvent(
            RequestId: request.Id,
            RequestUid: request.GetRequestUid(),
            TestOrders: request.TestOrders.ToList());
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

    private void VerifyNotReportedIssueRemoved(int requestId)
    {
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<CreateRequestIssueCommand>(command => command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mediatorMock.Verify(
            x => x.Send(
                It.Is<DeleteRequestIssueCommand>(command =>
                    command.RequestId == requestId
                    && command.IssueType == RequestIssueType.NotReported),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Domain.Request CreateRequest(
        int requestId,
        RequestState requestState,
        IReadOnlyList<TestOrder> testOrders,
        RequestSource source = RequestSource.Lis)
    {
        var request = new Domain.Request
        {
            Id = requestId,
            SampleId = $"SAMPLE-{requestId}",
            Created = DateTimeOffset.UtcNow,
            RequestState = requestState,
            Source = source
        };

        foreach (var testOrder in testOrders)
        {
            testOrder.RequestId = requestId;
            testOrder.Request = request;
            request.TestOrders.Add(testOrder);
        }

        return request;
    }

    private static TestOrder CreateTestOrder(TestOrderState state, int? lisChannelId, string methodName = "Method")
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
        LisAutoExportTrigger? channel2Trigger = null,
        WhatToExport whatToExport = WhatToExport.AllStatuses,
        bool includeLocallyEnteredSamples = false)
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
                        AutomaticSampleExport = channel0Trigger,
                        WhatToExport = whatToExport,
                        IncludeLocallyEnteredSamples = includeLocallyEnteredSamples
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
                        AutomaticSampleExport = channel2Trigger.Value,
                        WhatToExport = whatToExport,
                        IncludeLocallyEnteredSamples = includeLocallyEnteredSamples
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
