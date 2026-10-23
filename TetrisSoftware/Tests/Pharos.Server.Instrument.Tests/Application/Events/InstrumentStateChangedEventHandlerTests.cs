using MediatR;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Services;

namespace Pharos.Server.Instrument.Application.Events;

public class InstrumentStateChangedEventHandlerTests
{
    private const int _instrumentId = 1;
    private const string _instrumentNumber = "INST001";

    private readonly Mock<IInstrumentManager> _instrumentManagerMock = new();
    private readonly Mock<IInstrumentHub> _instrumentHubMock = new();
    private readonly Mock<ICurrentInstrumentServerHub> _currentInstrumentHubMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<IAutomaticPowerSaveScheduler> _automaticPowerSaveSchedulerMock = new();
    private readonly InstrumentStateChangedEventHandler _sut;

    public InstrumentStateChangedEventHandlerTests()
    {
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_instrumentId))
            .Returns(new Domain.Instrument
            {
                Id = _instrumentId,
                SerialNumber = _instrumentNumber
            });

        _instrumentHubMock
            .Setup(x => x.InstrumentStateChanged(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _currentInstrumentHubMock
            .Setup(x => x.InstrumentStateChanged(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _senderMock
            .Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _automaticPowerSaveSchedulerMock
            .Setup(x => x.OnInstrumentStateChangedAsync(It.IsAny<int>(), It.IsAny<InstrumentState>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new InstrumentStateChangedEventHandler(
            _instrumentManagerMock.Object,
            _instrumentHubMock.Object,
            _currentInstrumentHubMock.Object,
            _senderMock.Object,
            _automaticPowerSaveSchedulerMock.Object);
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public async Task Handle_WhenInstrumentIsOffline_Should_CreateInstrumentSoftwareOfflineTaskCard()
    {
        await _sut.Handle(new InstrumentStateChangedEvent(_instrumentId, InstrumentState.Offline), TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(
            It.Is<TaskCardCommand>(command =>
                command.CardType == TaskCardType.InstrumentSoftwareOffline &&
                command.InstrumentNumber == _instrumentNumber &&
                command.Rules.Count == 1 &&
                command.Rules[0].CardState == TaskCardState.Error),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public async Task Handle_WhenInstrumentIsNotOffline_Should_RemoveInstrumentSoftwareOfflineTaskCard()
    {
        await _sut.Handle(new InstrumentStateChangedEvent(_instrumentId, InstrumentState.Synchronizing), TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(
            It.Is<RemoveTaskCardCommand>(command =>
                command.CardType == TaskCardType.InstrumentSoftwareOffline &&
                command.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
