using MediatR;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Reagents.Application.Queries.Inventory;
using Tetris.Protocol.Instrument.Move;

namespace Pharos.Server.Reagents.Application.Commands;

public class UnloadReagentCommandHandlerTests
{
    private const string _testInstrumentNumber1 = "INST001";
    private const string _testInstrumentNumber2 = "INST002";
    private const int _testPosition1 = 1;
    private const int _testPosition2 = 2;

    [Fact]
    public async Task Handle_SendsSingleUnloadReagentRequest_WhenSingleInstrumentProvided()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.InstrumentNumber == _testInstrumentNumber1 &&
                    cmd.UnloadReagents.ReagentsToUnload.Count == 1),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SendsMultipleUnloadReagentRequests_WhenMultipleInstrumentsProvided()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber2, _testPosition1, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd => cmd.InstrumentNumber == _testInstrumentNumber1),
                TestContext.Current.CancellationToken),
            Times.Once);

        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd => cmd.InstrumentNumber == _testInstrumentNumber2),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GroupsReagentsByInstrumentNumber()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition2, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.InstrumentNumber == _testInstrumentNumber1 &&
                    cmd.UnloadReagents.ReagentsToUnload.Count == 2),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_1368")]
    [Trait("ModuleRequirement", "MR_4877")]
    [Trait("ProductRequirement", "PR_1368")]
    [Trait("ModuleRequirement", "MR_4901")]
    [Fact]
    public async Task Handle_PreservesAbortUnloadFlag()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, true, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.UnloadReagents.ReagentsToUnload[0].AbortUnload == true),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_2123")]
    [Trait("ModuleRequirement", "MR_4874")]
    [Trait("ProductRequirement", "PR_1923")]
    [Trait("ModuleRequirement", "MR_4898")]
    [Fact]
    public async Task Handle_PreservesPosition()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition2, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.UnloadReagents.ReagentsToUnload[0].Position == _testPosition2),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_2123")]
    [Trait("ModuleRequirement", "MR_4874")]
    [Trait("ProductRequirement", "PR_1923")]
    [Trait("ModuleRequirement", "MR_4898")]
    [Fact]
    public async Task Handle_ConvertsStorageAreaCorrectly()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var storageArea = 2;
        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, false, storageArea)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.UnloadReagents.ReagentsToUnload[0].StorageArea == (EnumReagentAutoUnloadStorageArea)storageArea),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotSendCommands_WhenEmptyListProvided()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>();
        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.IsAny<SendUnloadReagentRequestCommand>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MaintainsOrderOfReagentsPerInstrument()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, 1, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, 2, true, 1),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, 3, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.UnloadReagents.ReagentsToUnload.Count == 3 &&
                    cmd.UnloadReagents.ReagentsToUnload[0].Position == 1 &&
                    cmd.UnloadReagents.ReagentsToUnload[0].AbortUnload == false &&
                    cmd.UnloadReagents.ReagentsToUnload[1].Position == 2 &&
                    cmd.UnloadReagents.ReagentsToUnload[1].AbortUnload == true &&
                    cmd.UnloadReagents.ReagentsToUnload[2].Position == 3 &&
                    cmd.UnloadReagents.ReagentsToUnload[2].AbortUnload == false),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GroupsComplexScenarioCorrectly()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, 1, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber2, 1, true, 1),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, 2, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber2, 2, false, 1),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, 3, true, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.InstrumentNumber == _testInstrumentNumber1 &&
                    cmd.UnloadReagents.ReagentsToUnload.Count == 3),
                TestContext.Current.CancellationToken),
            Times.Once);

        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.InstrumentNumber == _testInstrumentNumber2 &&
                    cmd.UnloadReagents.ReagentsToUnload.Count == 2),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_HandlesCancellationToken()
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, cancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.IsAny<SendUnloadReagentRequestCommand>(),
                cancellationToken),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_2123")]
    [Trait("ModuleRequirement", "MR_4874")]
    [Trait("ProductRequirement", "PR_1923")]
    [Trait("ModuleRequirement", "MR_4898")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_WorksWithDifferentStorageAreas(int storageArea)
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, _testPosition1, false, storageArea)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.UnloadReagents.ReagentsToUnload[0].StorageArea == (EnumReagentAutoUnloadStorageArea)storageArea),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_2123")]
    [Trait("ModuleRequirement", "MR_4874")]
    [Trait("ProductRequirement", "PR_1923")]
    [Trait("ModuleRequirement", "MR_4898")]
    [Theory]
    [InlineData(1, 5, 10)]
    [InlineData(2, 3, 7)]
    public async Task Handle_WorksWithDifferentPositions(int position1, int position2, int position3)
    {
        // Arrange
        var sender = new Mock<ISender>();
        var handler = CreateHandler(sender);

        var items = new List<UnloadReagentRequestItemDto>
        {
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, position1, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, position2, false, 0),
            CreateUnloadReagentRequestItem(_testInstrumentNumber1, position3, false, 0)
        };

        var command = new UnloadReagentCommand(items);

        // Act
        await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        sender.Verify(
            s => s.Send(
                It.Is<SendUnloadReagentRequestCommand>(cmd =>
                    cmd.UnloadReagents.ReagentsToUnload.Count == 3 &&
                    cmd.UnloadReagents.ReagentsToUnload[0].Position == position1 &&
                    cmd.UnloadReagents.ReagentsToUnload[1].Position == position2 &&
                    cmd.UnloadReagents.ReagentsToUnload[2].Position == position3),
                TestContext.Current.CancellationToken),
            Times.Once);
    }

    private static UnloadReagentCommandHandler CreateHandler(Mock<ISender> sender)
    {
        return new UnloadReagentCommandHandler(sender.Object);
    }

    private static UnloadReagentRequestItemDto CreateUnloadReagentRequestItem(
        string instrumentNumber,
        int position,
        bool abortUnload,
        int storageArea)
    {
        return new UnloadReagentRequestItemDto
        {
            InstrumentNumber = instrumentNumber,
            Position = position,
            AbortUnload = abortUnload,
            StorageArea = (ReagentStorageArea)storageArea
        };
    }
}
