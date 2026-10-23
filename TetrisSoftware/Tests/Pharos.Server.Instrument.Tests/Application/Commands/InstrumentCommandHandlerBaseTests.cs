using MediatR;
using Moq;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Application.Commands;

public class InstrumentCommandHandlerBaseTests
{
    private const string _instrumentNumber = "instrument-1";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenSessionIsNotSynchronized_ShouldRespectAllowedIfNotSynchronized(bool allowedIfNotSynchronized)
    {
        // Arrange
        var sessionManager = CreateSessionManager(CreateSession());
        var sut = new TestCommandHandler(sessionManager.Object, allowedIfNotSynchronized, false, true);

        // Act
        var act = () => sut.Handle(new TestCommand(_instrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        if (allowedIfNotSynchronized)
        {
            await act();
            sut.WasHandled.Should().BeTrue();
        }
        else
        {
            await act.Should().ThrowAsync<NotAllowedException>();
            sut.WasHandled.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenSessionIsInLimitedMode_ShouldRespectAllowedInLimitedMode(bool allowedInLimitedMode)
    {
        // Arrange
        var sessionManager = CreateSessionManager(CreateSession(EnumCompatibilityStatus.FilesApiOnly));
        var sut = new TestCommandHandler(sessionManager.Object, false, allowedInLimitedMode, true);

        // Act
        var act = () => sut.Handle(new TestCommand(_instrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        if (allowedInLimitedMode)
        {
            await act();
            sut.WasHandled.Should().BeTrue();
        }
        else
        {
            await act.Should().ThrowAsync<NotAllowedException>();
            sut.WasHandled.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WhenSessionIsUnavailable_ShouldRespectThrowIfNotAllowed(bool throwIfNotAllowed)
    {
        // Arrange
        var sessionManager = new Mock<ICommunicationSessionManager>();
        var sut = new TestCommandHandler(sessionManager.Object, false, false, throwIfNotAllowed);

        // Act
        var act = () => sut.Handle(new TestCommand(_instrumentNumber), TestContext.Current.CancellationToken);

        // Assert
        if (throwIfNotAllowed)
        {
            await act.Should().ThrowAsync<NotAllowedException>();
        }
        else
        {
            await act();
        }

        sut.WasHandled.Should().BeFalse();
    }

    private static Mock<ICommunicationSessionManager> CreateSessionManager(CommunicationSession session)
    {
        var sessionManager = new Mock<ICommunicationSessionManager>();
        sessionManager.Setup(manager => manager.GetSession(_instrumentNumber)).Returns(session);
        return sessionManager;
    }

    private static CommunicationSession CreateSession(EnumCompatibilityStatus compatibilityStatus = EnumCompatibilityStatus.FullyCompatible)
    {
        return new CommunicationSession
        {
            CompatibilityStatus = compatibilityStatus,
            Connection = new Mock<IInstrumentConnection>().Object,
            InstrumentNumber = _instrumentNumber,
            SessionId = Guid.NewGuid()
        };
    }

    private sealed record TestCommand(string InstrumentNumber) : IInstrumentCommand, IRequest;

    private sealed class TestCommandHandler(
        ICommunicationSessionManager sessionManager,
        bool allowedIfNotSynchronized,
        bool allowedInLimitedMode,
        bool throwIfNotAllowed) : InstrumentCommandHandlerBase<TestCommand>(sessionManager)
    {
        public bool WasHandled { get; private set; }

        protected override bool AllowedIfNotSynchronized => allowedIfNotSynchronized;
        protected override bool AllowedInLimitedMode => allowedInLimitedMode;
        protected override bool ThrowIfNotAllowed => throwIfNotAllowed;

        protected override Task Handle(TestCommand request, CommunicationSession session, CancellationToken cancellationToken)
        {
            WasHandled = true;
            return Task.CompletedTask;
        }
    }
}
