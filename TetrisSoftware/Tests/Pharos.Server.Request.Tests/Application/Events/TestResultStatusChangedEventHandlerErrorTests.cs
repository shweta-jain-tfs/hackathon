using Microsoft.Extensions.Options;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;

namespace Pharos.Server.Request.Application.Events;

public class TestResultStatusChangedEventHandlerErrorTests
{
    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenAutomaticReflexAddsFollowUpTest_CreatesInformationError()
    {
        var dbContext = new Mock<IRequestDbContext>();
        var mediator = new Mock<IMediator>();
        var displayNameProvider = new Mock<ITestDisplayNameProvider>();
        var settings = new Mock<IOptionsSnapshot<RequestSettings>>();
        settings.SetupGet(x => x.Value).Returns(new RequestSettings());

        var request = new Domain.Request
        {
            Id = 1,
            SampleId = "Sample-1",
            RequestId = "Request-1",
            TestOrders = []
        };
        var reflex = new ReflexDefinition
        {
            Id = 10,
            Name = "Automatic reflex",
            Automatic = true,
            ReflexCondition = new ReflexCondition
            {
                ResultType = ResultType.CutOff,
                CompareType = CompareType.EqualTo,
                CutOffCompareValue = "Positive"
            },
            ReflexTests =
            [
                new ReflexTest { MethodName = "Method2", TestName = "FollowUp", TestSortOrder = 1 }
            ]
        };
        var triggerTest = new Domain.TestOrder
        {
            Uid = Guid.NewGuid(),
            RequestId = request.Id,
            Request = request,
            MethodName = "Method1",
            TestName = "Trigger",
            State = TestOrderState.PendingApproval,
            ReflexDefinitions = [reflex]
        };
        request.TestOrders.Add(triggerTest);

        dbContext.Setup(x => x.TestOrders).ReturnsDbSet([triggerTest]);
        dbContext.Setup(x => x.Requests).ReturnsDbSet([request]);
        dbContext.Setup(x => x.ReflexDefinitions).ReturnsDbSet([reflex]);
        dbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        displayNameProvider.Setup(x => x.GetTestDisplayName("Method2", "FollowUp")).ReturnsAsync("Follow up");
        mediator.Setup(x => x.Send(
                It.Is<GetReflexDefinitionsForTestQuery>(query =>
                    query.MethodName == "Method2" && query.TestName == "FollowUp"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mediator.Setup(x => x.Send(It.IsAny<CreatePharosErrorCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mediator.Setup(x => x.Send(It.IsAny<SyncTestOrdersWithInstrumentCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mediator.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = new Mock<ITestResult>();
        result.SetupAllProperties();
        result.Object.TestOrderId = triggerTest.Uid;
        result.Object.Status = TestResultStatus.Approved;
        result.Object.MeanCutOff = "Positive";

        var handler = new TestResultStatusChangedEventHandler(
            dbContext.Object,
            mediator.Object,
            displayNameProvider.Object,
            settings.Object);

        await handler.Handle(
            new TestResultStatusChangedEvent([result.Object]),
            TestContext.Current.CancellationToken);

        request.TestOrders.Should().Contain(x => x.IsReflexTest && x.TestName == "FollowUp");
        mediator.Verify(x => x.Send(
            It.Is<CreatePharosErrorCommand>(command =>
                command.ErrorCode == PharosErrorCodes.AutomaticFollowUpTestsAdded &&
                command.Classification == ErrorClassification.Information &&
                command.InstrumentNumber == null &&
                command.AllowMultiple &&
                command.Parameters != null &&
                command.Parameters.SequenceEqual(new[]
                {
                    "Automatic reflex",
                    "Sample-1",
                    "Request-1",
                    "Method1/Trigger",
                    "Method2/FollowUp"
                })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}