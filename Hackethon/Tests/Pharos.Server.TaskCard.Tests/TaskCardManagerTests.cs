using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.TaskCard.Application.Interfaces;
using Pharos.Server.TaskCard.Application.Services;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Control;
using Xunit;

namespace Pharos.Server.TaskCard.Tests;

public class TaskCardManagerTests
{
    [Trait("ProductRequirement", "PR_297")]
    [Trait("ModuleRequirement", "MR_4100")]
    [Fact]
    public async Task TaskCardsChanged_FirstEvent_ShouldSendMassControlSignalTowerCommand()
    {
        var taskCardServiceMock = new Mock<ITaskCardService>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var senderMock = new Mock<ISender>();
        using var sut = CreateSut(taskCardServiceMock, instrumentServiceMock, senderMock);

        instrumentServiceMock.Setup(x => x.GetInstruments()).Returns([]);
        taskCardServiceMock.Setup(x => x.GetControlSignalTowerRequests()).Returns(CreateControlTower(EnumSignalTowerLightState.Red, EnumSignalTowerControlSoundState.SoundShortSignal));
        senderMock
            .Setup(x => x.Send(It.IsAny<MassControlSignalTowerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await InvokeTaskCardsChangedAsync(sut);

        senderMock.Verify(x => x.Send(It.IsAny<MassControlSignalTowerCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Trait("ProductRequirement", "PR_297")]
    [Trait("ModuleRequirement", "MR_4100")]
    [Fact]
    public async Task TaskCardsChanged_SameTowerStateTwice_ShouldSendCommandBothTimes()
    {
        var taskCardServiceMock = new Mock<ITaskCardService>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var senderMock = new Mock<ISender>();
        using var sut = CreateSut(taskCardServiceMock, instrumentServiceMock, senderMock);

        instrumentServiceMock.Setup(x => x.GetInstruments()).Returns([]);
        taskCardServiceMock.Setup(x => x.GetControlSignalTowerRequests())
            .Returns(CreateControlTower(EnumSignalTowerLightState.Red, EnumSignalTowerControlSoundState.SoundShortSignal));
        senderMock
            .Setup(x => x.Send(It.IsAny<MassControlSignalTowerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await InvokeTaskCardsChangedAsync(sut);
        await InvokeTaskCardsChangedAsync(sut);

        senderMock.Verify(x => x.Send(It.IsAny<MassControlSignalTowerCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Trait("ProductRequirement", "PR_297")]
    [Trait("ModuleRequirement", "MR_4100")]
    [Fact]
    public async Task TaskCardsChanged_ChangedTowerState_ShouldSendAgain()
    {
        var taskCardServiceMock = new Mock<ITaskCardService>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var senderMock = new Mock<ISender>();
        using var sut = CreateSut(taskCardServiceMock, instrumentServiceMock, senderMock);

        instrumentServiceMock.Setup(x => x.GetInstruments()).Returns([]);
        taskCardServiceMock.SetupSequence(x => x.GetControlSignalTowerRequests())
            .Returns(CreateControlTower(EnumSignalTowerLightState.Red, EnumSignalTowerControlSoundState.SoundShortSignal))
            .Returns(CreateControlTower(EnumSignalTowerLightState.Green, EnumSignalTowerControlSoundState.SoundOff));
        senderMock
            .Setup(x => x.Send(It.IsAny<MassControlSignalTowerCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await InvokeTaskCardsChangedAsync(sut);
        await InvokeTaskCardsChangedAsync(sut);

        senderMock.Verify(x => x.Send(It.IsAny<MassControlSignalTowerCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Trait("ProductRequirement", "PR_297")]
    [Trait("ModuleRequirement", "MR_4100")]
    [Fact]
    public async Task TaskCardCreatedOrChanged_InstrumentSpecificTaskCard_ShouldNotifyCurrentInstrumentHub()
    {
        var taskCardServiceMock = new Mock<ITaskCardService>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var senderMock = new Mock<ISender>();
        var currentInstrumentServerHubMock = new Mock<ICurrentInstrumentServerHub>();
        using var sut = CreateSut(taskCardServiceMock, instrumentServiceMock, senderMock, currentInstrumentServerHubMock);
        var taskCard = CreateTaskCard(TaskCardState.Error, "INST001");

        instrumentServiceMock.Setup(x => x.FindInstrument("INST001")).Returns(CreateInstrument(42, "INST001"));
        currentInstrumentServerHubMock
            .Setup(x => x.TaskCardCreatedOrChanged(42, (int)TaskCardType.Error, (int)TaskCardState.Error))
            .Returns(Task.CompletedTask);

        await InvokeTaskCardCreatedOrChangedAsync(sut, taskCard);

        currentInstrumentServerHubMock.Verify(
            x => x.TaskCardCreatedOrChanged(42, (int)TaskCardType.Error, (int)TaskCardState.Error),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_297")]
    [Trait("ModuleRequirement", "MR_4100")]
    [Fact]
    public async Task TaskCardCreatedOrChanged_AllInstrumentTaskCard_ShouldNotifyAllCurrentInstrumentHubs()
    {
        var taskCardServiceMock = new Mock<ITaskCardService>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var senderMock = new Mock<ISender>();
        var currentInstrumentServerHubMock = new Mock<ICurrentInstrumentServerHub>();
        using var sut = CreateSut(taskCardServiceMock, instrumentServiceMock, senderMock, currentInstrumentServerHubMock);
        var taskCard = CreateTaskCard(TaskCardState.Warning, null);

        instrumentServiceMock.Setup(x => x.GetInstruments()).Returns([
            CreateInstrument(42, "INST001"),
            CreateInstrument(43, "INST002")
        ]);
        instrumentServiceMock.Setup(x => x.FindInstrument("INST001")).Returns(CreateInstrument(42, "INST001"));
        instrumentServiceMock.Setup(x => x.GetInstrument("INST002")).Returns(CreateInstrument(43, "INST002"));
        currentInstrumentServerHubMock
            .Setup(x => x.TaskCardCreatedOrChanged(It.IsAny<int>(), (int)TaskCardType.Error, (int)TaskCardState.Warning))
            .Returns(Task.CompletedTask);

        await InvokeTaskCardCreatedOrChangedAsync(sut, taskCard);

        currentInstrumentServerHubMock.Verify(
            x => x.TaskCardCreatedOrChanged(42, (int)TaskCardType.Error, (int)TaskCardState.Warning),
            Times.Once);
        currentInstrumentServerHubMock.Verify(
            x => x.TaskCardCreatedOrChanged(43, (int)TaskCardType.Error, (int)TaskCardState.Warning),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_297")]
    [Trait("ModuleRequirement", "MR_4100")]
    [Fact]
    public async Task TaskCardCreatedOrChanged_SystemErrorTaskCard_ShouldNotifyCurrentInstrumentHubAsError()
    {
        var taskCardServiceMock = new Mock<ITaskCardService>();
        var instrumentServiceMock = new Mock<IInstrumentManager>();
        var senderMock = new Mock<ISender>();
        var currentInstrumentServerHubMock = new Mock<ICurrentInstrumentServerHub>();
        using var sut = CreateSut(taskCardServiceMock, instrumentServiceMock, senderMock, currentInstrumentServerHubMock);
        var taskCard = CreateTaskCard(TaskCardState.Error, "INST001", TaskCardType.SystemError);

        instrumentServiceMock.Setup(x => x.FindInstrument("INST001")).Returns(CreateInstrument(42, "INST001"));
        currentInstrumentServerHubMock
            .Setup(x => x.TaskCardCreatedOrChanged(42, (int)TaskCardType.Error, (int)TaskCardState.Error))
            .Returns(Task.CompletedTask);

        await InvokeTaskCardCreatedOrChangedAsync(sut, taskCard);

        currentInstrumentServerHubMock.Verify(
            x => x.TaskCardCreatedOrChanged(42, (int)TaskCardType.Error, (int)TaskCardState.Error),
            Times.Once);
        currentInstrumentServerHubMock.Verify(
            x => x.TaskCardCreatedOrChanged(42, (int)TaskCardType.SystemError, It.IsAny<int>()),
            Times.Never);
    }

    private static Task InvokeTaskCardsChangedAsync(TaskCardManager sut)
    {
        var method = typeof(TaskCardManager).GetMethod(
            "TaskCardServiceOnTaskCardsChanged",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = method.Invoke(sut, null);
        return result is Task task ? task : Task.CompletedTask;
    }

    private static Task InvokeTaskCardCreatedOrChangedAsync(TaskCardManager sut, Domain.TaskCard taskCard)
    {
        var method = typeof(TaskCardManager).GetMethod(
            "TaskCardServiceOnTaskCardCreatedOrChanged",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = method.Invoke(sut, [taskCard]);
        return result is Task task ? task : Task.CompletedTask;
    }

    private static Domain.TaskCard CreateTaskCard(TaskCardState taskCardState, string? instrumentNumber, TaskCardType taskCardType = TaskCardType.Error)
    {
        var taskCard = new Domain.TaskCard([new Domain.TriggerState(taskCardState, DateTimeOffset.UtcNow)])
        {
            CardType = taskCardType,
            InstrumentNumber = instrumentNumber
        };

        taskCard.SetCurrentCardState(DateTimeOffset.UtcNow);

        return taskCard;
    }

    private static IInstrument CreateInstrument(int id, string serialNumber)
    {
        var instrumentMock = new Mock<IInstrument>();
        instrumentMock.SetupGet(x => x.Id).Returns(id);
        instrumentMock.SetupGet(x => x.SerialNumber).Returns(serialNumber);
        return instrumentMock.Object;
    }

    private static TaskCardManager CreateSut(
        Mock<ITaskCardService> taskCardServiceMock,
        Mock<IInstrumentManager> instrumentServiceMock,
        Mock<ISender> senderMock,
        Mock<ICurrentInstrumentServerHub>? currentInstrumentServerHubMock = null)
    {
        var taskCardHubMock = new Mock<ITaskCardHub>();
        taskCardHubMock.Setup(x => x.TaskCardsUpdated()).Returns(Task.CompletedTask);

        currentInstrumentServerHubMock ??= new Mock<ICurrentInstrumentServerHub>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(ITaskCardHub))).Returns(taskCardHubMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(ICurrentInstrumentServerHub))).Returns(currentInstrumentServerHubMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(ISender))).Returns(senderMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<ILogger<TaskCardManager>>();

        return new TaskCardManager(taskCardServiceMock.Object, instrumentServiceMock.Object, scopeFactoryMock.Object, loggerMock.Object);
    }

    private static ControlTower CreateControlTower(EnumSignalTowerLightState light, EnumSignalTowerControlSoundState sound)
    {
        return new ControlTower(new Dictionary<string, ControlSignalTowerRequest>
        {
            ["INST001"] = new ControlSignalTowerRequest
            {
                SetLight = light,
                SetSound = sound
            }
        }, null);
    }
}
