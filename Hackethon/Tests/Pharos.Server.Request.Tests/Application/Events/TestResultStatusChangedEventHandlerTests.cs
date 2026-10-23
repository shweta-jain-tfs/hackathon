using Microsoft.Extensions.Options;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Application.Commands.ProcessLisExport;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Result.Domain;

namespace Pharos.Server.Request.Application.Events;

public class TestResultStatusChangedEventHandlerTests
{
    private readonly Mock<IRequestDbContext> _dbContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITestDisplayNameProvider> _displayNameProviderMock = new();
    private readonly Mock<IOptionsSnapshot<RequestSettings>> _requestSettingsMock = new();

    public TestResultStatusChangedEventHandlerTests()
    {
        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ProcessLisExportCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _displayNameProviderMock
            .Setup(x => x.GetTestDisplayName(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string _, string testName) => testName);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6029")]
    public async Task Handle_WhenAddedReflexTestMeetsOwnReflexCondition_AddsNextFollowUpTest()
    {
        // Arrange
        _requestSettingsMock.SetupGet(x => x.Value).Returns(new RequestSettings
        {
            PreventDuplicateReflexTests = true
        });

        var request = CreateRequestWithAutomaticReflex("TestA", "TestB");
        var testA = request.TestOrders.Single();

        SetupReflexDefinitionQuery("TestB", CreateAutomaticReflexDefinition("TestB", "TestC"));
        SetupReflexDefinitionQuery("TestC");
        SetupRequestDbContext(request);

        var sut = CreateSut();

        // Act
        await sut.Handle(CreateApprovedResult(testA), TestContext.Current.CancellationToken);

        var testB = request.TestOrders.Single(x => x.TestName == "TestB");
        SetRequestRelationship(testB, request);
        SetupRequestDbContext(request);

        await sut.Handle(CreateApprovedResult(testB), TestContext.Current.CancellationToken);

        // Assert
        request.TestOrders.Should().ContainSingle(x => x.TestName == "TestA");
        request.TestOrders.Should().ContainSingle(x => x.TestName == "TestB" && x.IsReflexTest);
        request.TestOrders.Should().ContainSingle(x => x.TestName == "TestC" && x.IsReflexTest);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6029")]
    public async Task Handle_WhenChainedReflexWouldAddExistingTest_DoesNotCreateCycle()
    {
        // Arrange
        _requestSettingsMock.SetupGet(x => x.Value).Returns(new RequestSettings
        {
            PreventDuplicateReflexTests = true
        });

        var request = CreateRequestWithAutomaticReflex("TestA", "TestB");
        var testA = request.TestOrders.Single();

        SetupReflexDefinitionQuery("TestB", CreateAutomaticReflexDefinition("TestB", "TestA"));
        SetupRequestDbContext(request);

        var sut = CreateSut();

        // Act
        await sut.Handle(CreateApprovedResult(testA), TestContext.Current.CancellationToken);

        var testB = request.TestOrders.Single(x => x.TestName == "TestB");
        SetRequestRelationship(testB, request);
        SetupRequestDbContext(request);

        await sut.Handle(CreateApprovedResult(testB), TestContext.Current.CancellationToken);

        // Assert
        request.TestOrders.Should().HaveCount(2);
        request.TestOrders.Should().ContainSingle(x => x.TestName == "TestA");
        request.TestOrders.Should().ContainSingle(x => x.TestName == "TestB");
    }

    private TestResultStatusChangedEventHandler CreateSut()
    {
        return new TestResultStatusChangedEventHandler(
            _dbContextMock.Object,
            _mediatorMock.Object,
            _displayNameProviderMock.Object,
            _requestSettingsMock.Object);
    }

    private void SetupRequestDbContext(Domain.Request request)
    {
        var testOrders = request.TestOrders.ToList();
        var reflexDefinitions = testOrders
            .SelectMany(x => x.ReflexDefinitions ?? [])
            .ToList();

        _dbContextMock.Setup(x => x.Requests).ReturnsDbSet([request]);
        _dbContextMock.Setup(x => x.TestOrders).ReturnsDbSet(testOrders);
        _dbContextMock.Setup(x => x.ReflexDefinitions).ReturnsDbSet(reflexDefinitions);
    }

    private void SetupReflexDefinitionQuery(string testName, params IReflexDefinition[] definitions)
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetReflexDefinitionsForTestQuery>(query =>
                    query.MethodName == "Method1" && query.TestName == testName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(definitions.ToList());
    }

    private static Domain.Request CreateRequestWithAutomaticReflex(string triggerTestName, string followUpTestName)
    {
        var request = new Domain.Request
        {
            Id = 1,
            SampleId = "Sample-1",
            TestOrders = []
        };

        var testOrder = new Domain.TestOrder
        {
            Uid = Guid.NewGuid(),
            RequestId = request.Id,
            Request = request,
            MethodName = "Method1",
            TestName = triggerTestName,
            TestSortOrder = 1,
            State = TestOrderState.PendingApproval,
            ReflexDefinitions = [CreateAutomaticReflexDefinition(triggerTestName, followUpTestName)]
        };

        request.TestOrders.Add(testOrder);
        return request;
    }

    private static ReflexDefinition CreateAutomaticReflexDefinition(string triggerTestName, string followUpTestName)
    {
        return new ReflexDefinition
        {
            Name = $"{triggerTestName} to {followUpTestName}",
            Automatic = true,
            ReflexCondition = new ReflexCondition
            {
                MethodName = "Method1",
                TestName = triggerTestName,
                ResultType = ResultType.Concentration,
                CompareType = CompareType.Above,
                ConcentrationCompareValue = 1
            },
            ReflexTests =
            [
                new ReflexTest
                {
                    MethodName = "Method1",
                    TestName = followUpTestName,
                    NumberOfReplicates = 1,
                    InstrumentDilutionFactor = 1,
                    TestSortOrder = 1
                }
            ]
        };
    }

    private static TestResultStatusChangedEvent CreateApprovedResult(Domain.TestOrder testOrder)
    {
        return new TestResultStatusChangedEvent(
        [
            new SampleTestResult
            {
                TestOrderId = testOrder.Uid,
                MethodName = testOrder.MethodName,
                TestName = testOrder.TestName,
                Status = TestResultStatus.Approved,
                MeanConcentration = 2
            }
        ]);
    }

    private static void SetRequestRelationship(Domain.TestOrder testOrder, Domain.Request request)
    {
        // The real RequestDbContext performs this relationship fix-up when the child is added.
        // ReturnsDbSet does not track relationships, so the unit test must mirror it explicitly.
        testOrder.Request = request;
        testOrder.RequestId = request.Id;
    }
}
