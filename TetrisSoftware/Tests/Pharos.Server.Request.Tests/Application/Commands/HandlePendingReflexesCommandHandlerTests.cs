using Microsoft.Extensions.Options;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;

namespace Pharos.Server.Request.Application.Commands;

public class HandlePendingReflexesCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITestDisplayNameProvider> _displayNameProviderMock = new();
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock = new();
    private readonly HandlePendingReflexesCommandHandler _sut;

    public HandlePendingReflexesCommandHandlerTests()
    {
        _requestSettingsMock.SetupGet(x => x.Value).Returns(new RequestSettings());

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _dbContextMock
            .Setup(x => x.HasChanges())
            .Returns(true);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreatePharosErrorCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetReflexDefinitionsForTestQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _sut = new HandlePendingReflexesCommandHandler(
            _dbContextMock.Object,
            _mediatorMock.Object,
            _displayNameProviderMock.Object,
            _requestSettingsMock.Object);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenSomeReflexDefinitionsRemainAfterHandling_KeepsApprovedPendingReflexState()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = new Domain.Request
        {
            Id = 1,
            SampleId = "Sample-1",
            TestOrders = []
        };

        var reflexToHandle = new ReflexDefinition { Id = 10, Name = "ManualReflex1", Automatic = false, ReflexCondition = new Pharos.Server.Domain.Reflex.ReflexCondition() };
        var reflexToKeep = new ReflexDefinition { Id = 20, Name = "ManualReflex2", Automatic = false, ReflexCondition = new Pharos.Server.Domain.Reflex.ReflexCondition() };

        var testOrder = new Domain.TestOrder
        {
            Uid = testOrderUid,
            RequestId = request.Id,
            Request = request,
            MethodName = "Method1",
            TestName = "Test1",
            TestSortOrder = 1,
            State = TestOrderState.ApprovedPendingReflex,
            ReflexDefinitions = [reflexToHandle, reflexToKeep]
        };

        request.TestOrders.Add(testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet([reflexToHandle, reflexToKeep]);

        var command = new HandlePendingReflexesCommand([10], true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        testOrder.State.Should().Be(TestOrderState.ApprovedPendingReflex);
        testOrder.ReflexDefinitions.Should().HaveCount(1);
        testOrder.ReflexDefinitions!.Single().Id.Should().Be(20);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.IsAny<PendingReflexTestsHandledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(x => x.Send(
            It.Is<CreatePharosErrorCommand>(c =>
                c.ErrorCode == PharosErrorCodes.ManualFollowUpTestsDiscarded &&
                c.Classification == ErrorClassification.Information &&
                c.InstrumentNumber == null &&
                c.AllowMultiple &&
                c.Parameters != null &&
                c.Parameters.SequenceEqual(new[]
                {
                    "ManualReflex1",
                    "Sample-1",
                    string.Empty,
                    "Method1/Test1",
                    string.Empty
                })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenAllReflexDefinitionsAreHandled_SetsApprovedState()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var request = new Domain.Request
        {
            Id = 2,
            SampleId = "Sample-2",
            TestOrders = []
        };

        var onlyReflex = new ReflexDefinition { Id = 30, Name = "ManualReflex", Automatic = false, ReflexCondition = new Pharos.Server.Domain.Reflex.ReflexCondition() };

        var testOrder = new Domain.TestOrder
        {
            Uid = testOrderUid,
            RequestId = request.Id,
            Request = request,
            MethodName = "Method2",
            TestName = "Test2",
            TestSortOrder = 1,
            State = TestOrderState.ApprovedPendingReflex,
            ReflexDefinitions = [onlyReflex]
        };

        request.TestOrders.Add(testOrder);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([testOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet([onlyReflex]);

        var command = new HandlePendingReflexesCommand([30], true);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        testOrder.State.Should().Be(TestOrderState.Approved);
        testOrder.ReflexDefinitions.Should().BeEmpty();
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(
            It.Is<PendingReflexTestsHandledEvent>(e => e.TestOrderIds.SequenceEqual(new[] { testOrderUid })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6029")]
    public async Task Handle_WhenPendingReflexBelongsToReflexTest_AddsFollowUpWithFreshReflexDefinitions()
    {
        // Arrange
        var triggerTestOrderUid = Guid.NewGuid();
        var request = new Domain.Request
        {
            Id = 3,
            SampleId = "Sample-3",
            TestOrders = []
        };

        var pendingReflex = new ReflexDefinition
        {
            Id = 40,
            Name = "ManualReflex",
            Automatic = false,
            ReflexCondition = new ReflexCondition(),
            ReflexTests =
            [
                new ReflexTest
                {
                    MethodName = "Method2",
                    TestName = "Test2",
                    TestSortOrder = 1
                }
            ]
        };

        var triggerTestOrder = new Domain.TestOrder
        {
            Uid = triggerTestOrderUid,
            RequestId = request.Id,
            Request = request,
            MethodName = "Method1",
            TestName = "Test1",
            TestSortOrder = 1,
            State = TestOrderState.ApprovedPendingReflex,
            IsReflexTest = true,
            ReflexDefinitions = [pendingReflex]
        };

        request.TestOrders.Add(triggerTestOrder);

        var chainedReflexDefinition = new ReflexDefinition
        {
            Id = 50,
            Name = "ChainedReflex",
            Automatic = true,
            ReflexCondition = new ReflexCondition
            {
                MethodName = "Method2",
                TestName = "Test2"
            },
            ReflexTests =
            [
                new ReflexTest
                {
                    MethodName = "Method3",
                    TestName = "Test3"
                }
            ]
        };

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([triggerTestOrder]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet([pendingReflex]);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetReflexDefinitionsForTestQuery>(query =>
                    query.MethodName == "Method2" && query.TestName == "Test2"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IReflexDefinition> { chainedReflexDefinition });

        _displayNameProviderMock
            .Setup(x => x.GetTestDisplayName("Method2", "Test2"))
            .ReturnsAsync("Test 2");

        var command = new HandlePendingReflexesCommand([40], false);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        triggerTestOrder.State.Should().Be(TestOrderState.Approved);
        var addedTestOrder = request.TestOrders.Single(x => x.Uid != triggerTestOrderUid);
        addedTestOrder.IsReflexTest.Should().BeTrue();
        addedTestOrder.MethodName.Should().Be("Method2");
        addedTestOrder.TestName.Should().Be("Test2");
        addedTestOrder.ReflexDefinitions.Should().ContainSingle();
        addedTestOrder.ReflexDefinitions!.Single().Id.Should().Be(0);
        addedTestOrder.ReflexDefinitions.Single().Name.Should().Be("ChainedReflex");
        addedTestOrder.ReflexDefinitions.Single().ReflexTests.Should().ContainSingle(x =>
            x.MethodName == "Method3" && x.TestName == "Test3");
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenFollowUpTestIsAdded_CreatesManualAddInformationError()
    {
        var request = new Domain.Request
        {
            Id = 3,
            SampleId = "Sample-3",
            RequestId = "Request-3",
            TestOrders = []
        };
        var reflex = new ReflexDefinition
        {
            Id = 40,
            Name = "ManualReflex",
            Automatic = false,
            ReflexCondition = new Pharos.Server.Domain.Reflex.ReflexCondition(),
            ReflexTests =
            [
                new Pharos.Server.Domain.Reflex.ReflexTest
                {
                    MethodName = "Method2",
                    TestName = "FollowUp",
                    TestSortOrder = 1
                }
            ]
        };
        var triggerTest = new Domain.TestOrder
        {
            Uid = Guid.NewGuid(),
            RequestId = request.Id,
            Request = request,
            MethodName = "Method1",
            TestName = "Trigger",
            State = TestOrderState.ApprovedPendingReflex,
            ReflexDefinitions = [reflex]
        };
        request.TestOrders.Add(triggerTest);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([triggerTest]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet([reflex]);
        _displayNameProviderMock.Setup(x => x.GetTestDisplayName("Method2", "FollowUp")).ReturnsAsync("Follow up");

        await _sut.Handle(new HandlePendingReflexesCommand([40], false), TestContext.Current.CancellationToken);

        request.TestOrders.Should().Contain(x => x.IsReflexTest && x.TestName == "FollowUp");
        _mediatorMock.Verify(x => x.Send(
            It.Is<CreatePharosErrorCommand>(c =>
                c.ErrorCode == PharosErrorCodes.ManualFollowUpTestsAdded &&
                c.Classification == ErrorClassification.Information &&
                c.InstrumentNumber == null &&
                c.AllowMultiple &&
                c.Parameters != null &&
                c.Parameters.SequenceEqual(new[]
                {
                    "ManualReflex",
                    "Sample-3",
                    "Request-3",
                    "Method1/Trigger",
                    "Method2/FollowUp"
                })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenAllFollowUpTestsAreDuplicates_DoesNotCreateManualAddInformationError()
    {
        _requestSettingsMock.SetupGet(x => x.Value).Returns(new RequestSettings { PreventDuplicateReflexTests = true });

        var request = new Domain.Request { Id = 4, SampleId = "Sample-4", TestOrders = [] };
        var reflex = new ReflexDefinition
        {
            Id = 50,
            Name = "ManualReflex",
            ReflexCondition = new Pharos.Server.Domain.Reflex.ReflexCondition(),
            ReflexTests =
            [
                new Pharos.Server.Domain.Reflex.ReflexTest { MethodName = "Method2", TestName = "Duplicate", TestSortOrder = 1 }
            ]
        };
        var triggerTest = new Domain.TestOrder
        {
            Uid = Guid.NewGuid(), RequestId = request.Id, Request = request,
            MethodName = "Method1", TestName = "Trigger", State = TestOrderState.ApprovedPendingReflex,
            ReflexDefinitions = [reflex]
        };
        var duplicate = new Domain.TestOrder
        {
            Uid = Guid.NewGuid(), RequestId = request.Id, Request = request,
            MethodName = "Method2", TestName = "Duplicate", State = TestOrderState.Approved
        };
        request.TestOrders.Add(triggerTest);
        request.TestOrders.Add(duplicate);

        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet([triggerTest, duplicate]);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet([reflex]);

        await _sut.Handle(new HandlePendingReflexesCommand([50], false), TestContext.Current.CancellationToken);

        _mediatorMock.Verify(x => x.Send(
            It.Is<CreatePharosErrorCommand>(c => c.ErrorCode == PharosErrorCodes.ManualFollowUpTestsAdded),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
