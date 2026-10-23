using MediatR;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.TaskCard;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Application.Events;

public class InstrumentEnabledChangedEventHandlerTests
{
    private const int _instrumentId = 1;
    private const string _instrumentNumber = "INST001";

    private readonly Mock<IInstrumentManager> _instrumentManagerMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ICurrentInstrumentServerHub> _currentInstrumentHubMock = new();
    private readonly Domain.Instrument _instrument = new()
    {
        Id = _instrumentId,
        SerialNumber = _instrumentNumber,
        Enabled = true,
        State = InstrumentState.Offline
    };
    private readonly InstrumentEnabledChangedEventHandler _sut;

    public InstrumentEnabledChangedEventHandlerTests()
    {
        _instrumentManagerMock
            .Setup(x => x.GetInstrument(_instrumentNumber))
            .Returns(_instrument);

        _currentInstrumentHubMock
            .Setup(x => x.InstrumentEnabledChanged(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        _senderMock
            .Setup(x => x.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new InstrumentEnabledChangedEventHandler(
            _instrumentManagerMock.Object,
            _senderMock.Object,
            _currentInstrumentHubMock.Object);
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public async Task Handle_WhenInstrumentEnabledChanged_Should_NotifyCurrentInstrumentHub()
    {
        await _sut.Handle(new InstrumentEnabledChangedEvent(_instrumentNumber, true), TestContext.Current.CancellationToken);

        _currentInstrumentHubMock.Verify(x => x.InstrumentEnabledChanged(_instrumentId, true), Times.Once);
    }

    [Trait("ProductRequirement", "PR_950")]
    [Fact]
    public async Task Handle_WhenEnabledInstrumentIsOffline_Should_CreateInstrumentSoftwareOfflineTaskCard()
    {
        await _sut.Handle(new InstrumentEnabledChangedEvent(_instrumentNumber, true), TestContext.Current.CancellationToken);

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
    public async Task Handle_WhenOfflineInstrumentIsDisabled_Should_RemoveInstrumentSoftwareOfflineTaskCard()
    {
        _instrument.Enabled = false;

        await _sut.Handle(new InstrumentEnabledChangedEvent(_instrumentNumber, false), TestContext.Current.CancellationToken);

        _senderMock.Verify(x => x.Send(
            It.Is<RemoveTaskCardCommand>(command =>
                command.CardType == TaskCardType.InstrumentSoftwareOffline &&
                command.InstrumentNumber == _instrumentNumber),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
