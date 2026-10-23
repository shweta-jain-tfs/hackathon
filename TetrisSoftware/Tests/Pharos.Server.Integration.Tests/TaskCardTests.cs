using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.TaskCard.Application.Interfaces;

namespace Pharos.Server.Integration.Tests;

[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestsCollection))]
public class TaskCardTests(IntegrationTestsWaf factory) : IntegrationTestBase(factory)
{
    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        // Remove any stale global task cards left by previous tests.
        // ITaskCardService is a singleton and GetTaskCardsForInstrument includes
        // global cards (ControlsAllInstruments == true), so a card left from
        // e.g. TubeDefinitionTests or a background sync (RequestReportResult)
        // would cause the final assertion to fail.
        var mediator = ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RemoveTaskCardCommand(TaskCardType.SystemError));
        await mediator.Send(new RemoveTaskCardCommand(TaskCardType.ConnectionFailure));
        await mediator.Send(new RemoveTaskCardCommand(TaskCardType.RequestSampleIssue));
        await mediator.Send(new RemoveTaskCardCommand(TaskCardType.RequestReportResult));
    }

    [Fact]
    public async Task CreateAndRemoveTaskCardByCommand()
    {
        //Arrange

        //Get needed services
        var mediator = ServiceProvider.GetService<IMediator>()!;
        var taskCardService = ServiceProvider.GetService<ITaskCardService>()!;

        //Act

        //Send command to add a task card
        await mediator.Send(new TaskCardCommand(TaskCardType.SampleDrawerOpen, TaskCardState.Information, "I1"), TestContext.Current.CancellationToken);

        //Get all task cards from service
        var taskCards = taskCardService.GetTaskCardsForInstrument("I1");

        //Assert that we have the task card we just created
        taskCards.Should().Contain(c => c.CardType == TaskCardType.SampleDrawerOpen);

        //Send new command to remove it from list
        await mediator.Send(new RemoveTaskCardCommand(TaskCardType.SampleDrawerOpen, "I1"), TestContext.Current.CancellationToken);

        //Get all task cards again
        taskCards = taskCardService.GetTaskCardsForInstrument("I1");

        //Assert that the task card we created was removed
        taskCards.Should().NotContain(c => c.CardType == TaskCardType.SampleDrawerOpen);
    }
}
