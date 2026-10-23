using MediatR;
using Moq;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Reflex.Domain;
using Xunit;

namespace Pharos.Server.Reflex.Application.Events;

public class ReflexDefinitionErrorEventHandlerTests
{
    private readonly Mock<ISender> _sender = new();

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenReflexDefinitionIsCreated_CreatesInformationError()
    {
        var definition = new ReflexDefinition { Name = "Reflex A" };
        var handler = new ReflexDefinitionErrorEventHandler(_sender.Object);

        await handler.Handle(new ReflexDefinitionCreatedEvent(definition), TestContext.Current.CancellationToken);

        _sender.Verify(x => x.Send(
            It.Is<CreatePharosErrorCommand>(command =>
                command.ErrorCode == PharosErrorCodes.ReflexDefinitionCreated &&
                command.Classification == ErrorClassification.Information &&
                command.InstrumentNumber == null &&
                command.AllowMultiple &&
                command.Parameters != null &&
                command.Parameters.SequenceEqual(new[] { "Reflex A" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_844")]
    [Trait("ModuleRequirement", "MR_6028")]
    public async Task Handle_WhenReflexDefinitionIsDeleted_CreatesInformationError()
    {
        var definition = new ReflexDefinition { Name = "Reflex B" };
        var handler = new ReflexDefinitionErrorEventHandler(_sender.Object);

        await handler.Handle(new ReflexDefinitionDeletedEvent(definition), TestContext.Current.CancellationToken);

        _sender.Verify(x => x.Send(
            It.Is<CreatePharosErrorCommand>(command =>
                command.ErrorCode == PharosErrorCodes.ReflexDefinitionDeleted &&
                command.Classification == ErrorClassification.Information &&
                command.InstrumentNumber == null &&
                command.AllowMultiple &&
                command.Parameters != null &&
                command.Parameters.SequenceEqual(new[] { "Reflex B" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
