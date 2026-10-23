using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Request.Application.Commands.RetestRejectedResultsCommand;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using RetestCommand = Pharos.Server.Application.Request.RetestRejectedResultsCommand;

namespace Pharos.Server.Request.Application.Commands;

public class RetestRejectedResultsCommandHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IMethodsService> _methodsServiceMock = new();
    private readonly RetestRejectedResultsCommandHandler _sut;

    private const string _methodName = "Method1";
    private const string _testName = "Test1";

    public RetestRejectedResultsCommandHandlerTests()
    {
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _methodsServiceMock
            .Setup(m => m.GetDefaultInstrumentDilutionFactor(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, 1, (AssayTechnology?)AssayTechnology.EliA));

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new RetestRejectedResultsCommandHandler(
            _dbContextMock.Object,
            _mediatorMock.Object,
            _methodsServiceMock.Object);
    }

    #region Valid states — a new test order is created

    public static TheoryData<TestOrderState> ValidStates => new()
    {
        TestOrderState.PendingApproval,
        TestOrderState.PendingBlank,
        TestOrderState.PendingCalibration,
        TestOrderState.Aborted,
        TestOrderState.Flagged,
    };

    [Theory]
    [MemberData(nameof(ValidStates))]
    public async Task Handle_WhenTestOrderIsInValidState_CreatesNewTestOrder(TestOrderState state)
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, state);

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        request.TestOrders.Should().HaveCount(2);
        var newOrder = request.TestOrders.Single(o => o.Uid != testOrderUid);
        newOrder.State.Should().Be(TestOrderState.NotStarted);
        newOrder.MethodName.Should().Be(_methodName);
        newOrder.TestName.Should().Be(_testName);
        newOrder.InstrumentNumber.Should().BeNull();
        newOrder.TubeId.Should().BeNull();

        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(ValidStates))]
    public async Task Handle_WhenTestOrderIsInValidState_PublishesRequestUpdatedEvent(TestOrderState state)
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, state);

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<RequestUpdatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [MemberData(nameof(ValidStates))]
    public async Task Handle_WhenTestOrderIsInValidState_SyncsWithInstrument(TestOrderState state)
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, state);

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Invalid states — NotFoundException is thrown

    public static TheoryData<TestOrderState> InvalidStates => new()
    {
        TestOrderState.NotStarted,
        TestOrderState.Scheduled,
        TestOrderState.Started,
        TestOrderState.Pipetted,
        TestOrderState.Approved,
        TestOrderState.ApprovedPendingReflex,
        TestOrderState.Rejected,
        TestOrderState.Reported,
    };

    [Theory]
    [MemberData(nameof(InvalidStates))]
    public async Task Handle_WhenTestOrderIsInInvalidState_ThrowsNotFoundException(TestOrderState state)
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, state);

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid]);

        // Act
        var act = () => _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Instrument dilution override

    [Fact]
    public async Task Handle_WhenInstrumentDilutionIsProvided_UsesProvidedDilution()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, TestOrderState.PendingApproval, instrumentDilution: 1);

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid], InstrumentDilution: 5);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var newOrder = request.TestOrders.Single(o => o.Uid != testOrderUid);
        newOrder.InstrumentDilutionFactor.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WhenInstrumentDilutionIsNotProvided_InheritsOriginalDilution()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, TestOrderState.PendingApproval, instrumentDilution: 10);

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var newOrder = request.TestOrders.Single(o => o.Uid != testOrderUid);
        newOrder.InstrumentDilutionFactor.Should().Be(10);
    }

    #endregion

    #region No matching test orders

    [Fact]
    public async Task Handle_WhenNoTestOrdersMatchIds_ThrowsNotFoundException()
    {
        // Arrange
        SetupDbContext([], []);

        var command = new RetestCommand([Guid.NewGuid()]);

        // Act
        var act = () => _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Multiple test orders across requests

    [Fact]
    public async Task Handle_WhenMultipleRequestsAreInvolved_SavesAndSyncsEachRequest()
    {
        // Arrange
        var uid1 = Guid.NewGuid();
        var uid2 = Guid.NewGuid();
        var (request1, testOrders1) = CreateRequestWithTestOrder(uid1, TestOrderState.PendingApproval, requestId: 1);
        var (request2, testOrders2) = CreateRequestWithTestOrder(uid2, TestOrderState.Flagged, requestId: 2);

        SetupDbContext([.. testOrders1, .. testOrders2], [request1, request2]);

        var command = new RetestCommand([uid1, uid2]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6029")]
    public async Task Handle_WhenRejectedTestIsReflexTest_MovesReflexDefinitionsToRetest()
    {
        // Arrange
        var testOrderUid = Guid.NewGuid();
        var (request, testOrders) = CreateRequestWithTestOrder(testOrderUid, TestOrderState.PendingApproval);
        var rejectedTestOrder = testOrders.Single();
        rejectedTestOrder.IsReflexTest = true;

        var reflexDefinition = new ReflexDefinition
        {
            Id = 10,
            TestOrderId = testOrderUid,
            Name = "ChainedReflex",
            Automatic = true,
            ReflexCondition = new Pharos.Server.Domain.Reflex.ReflexCondition(),
            ReflexTests =
            [
                new Pharos.Server.Domain.Reflex.ReflexTest
                {
                    MethodName = "Method2",
                    TestName = "Test2"
                }
            ]
        };
        rejectedTestOrder.ReflexDefinitions = [reflexDefinition];

        SetupDbContext(testOrders, [request]);

        var command = new RetestCommand([testOrderUid]);

        // Act
        await _sut.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var retest = request.TestOrders.Single(x => x.Uid != testOrderUid);
        retest.IsReflexTest.Should().BeTrue();
        reflexDefinition.TestOrderId.Should().Be(retest.Uid);
    }

    #endregion

    #region Helpers

    private static (Domain.Request Request, List<Domain.TestOrder> TestOrders) CreateRequestWithTestOrder(
        Guid testOrderUid,
        TestOrderState state,
        int instrumentDilution = 1,
        int requestId = 1)
    {
        var request = new Domain.Request
        {
            Id = requestId,
            SampleId = $"S{requestId:D5}",
            PreDilutionFactor = 1,
            TestOrders = []
        };

        var testOrder = new Domain.TestOrder
        {
            Uid = testOrderUid,
            State = state,
            MethodName = _methodName,
            TestName = _testName,
            NumberOfReplicates = 1,
            InstrumentDilutionFactor = instrumentDilution,
            RequestId = requestId,
            TestSortOrder = 1,
            TestPanelIds = [],
            IsReflexTest = false,
            Request = request
        };

        request.TestOrders.Add(testOrder);

        return (request, [testOrder]);
    }

    private void SetupDbContext(List<Domain.TestOrder> testOrders, List<Domain.Request> requests)
    {
        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(testOrders);
        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet(requests);
    }

    #endregion
}
