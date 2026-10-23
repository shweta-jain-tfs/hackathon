using MediatR;
using Moq;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.AuditTrail;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Commands.UnlockCompartment;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument;
using AuditTrailEventId = Pharos.Server.Application.AuditTrail.Enums;

namespace Pharos.Server.Instrument.Application.Commands
{
    public class UnlockCompartmentCommandHandlerTests
    {
        private readonly Mock<ICommunicationSessionManager> _sessionManagerMock = new();
        private readonly Mock<IInstrumentManager> _instrumentManagerMock = new();
        private readonly Mock<ISender> _senderMock = new();
        private readonly Mock<IAuthorizationService> _authServiceMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IDateTime> _dateTimeMock = new();

        private UnlockCompartmentCommandHandler CreateHandler()
        {
            return new UnlockCompartmentCommandHandler(
                _sessionManagerMock.Object,
                _instrumentManagerMock.Object,
                _senderMock.Object,
                _authServiceMock.Object,
                _currentUserServiceMock.Object,
                _dateTimeMock.Object
            );
        }

        private static Domain.Instrument CreateInstrument(string serial = "SN123", InstrumentState state = InstrumentState.Standby)
        {
            return new Domain.Instrument
            {
                SerialNumber = serial,
                State = state
            };
        }

        private static CommunicationSession CreateSession(string serial)
        {
            var connMock = new Mock<IInstrumentConnection>();
            connMock.Setup(c => c.UnlockCompartmentAsync(It.IsAny<UnlockCompartmentRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var session = new CommunicationSession
            {
                InstrumentNumber = serial,
                Connection = connMock.Object
            };
            session.SetSynchronizationFlag(CommunicationSynchronizationState.Synchronized);
            return session;
        }

        [Fact]
        public async Task Handle_AuthorizedReagentDrawer_UnlocksCompartmentAndSendsCommand()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            // Act
            await handler.Handle(command, TestContext.Current.CancellationToken);

            // Assert
            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer), It.IsAny<CancellationToken>()), Times.Once);
            _senderMock.Verify(s => s.Send(It.IsAny<CompartmentLockStateChangedCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AuthorizedSampleDrawer_UnlocksCompartment()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1);

            await handler.Handle(command, TestContext.Current.CancellationToken);

            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AuthorizedHood_Standby_UnlocksCompartment()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument(state: InstrumentState.Standby);
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageInstruments)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentHood);

            await handler.Handle(command, TestContext.Current.CancellationToken);

            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentHood), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AuthorizedHood_NotStandby_UnlocksCompartment()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument(state: InstrumentState.Assay);
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.RunServiceTools)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentHood);

            await handler.Handle(command, TestContext.Current.CancellationToken);

            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentHood), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Unauthorized_ThrowsNotAllowedException()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_MissingUser_ThrowsInvalidOperationException()
        {
            _currentUserServiceMock.SetupGet(x => x.UserId).Returns((Guid?)null);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand("SN123", EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_UnspecifiedCompartment_ThrowsNotAllowedException()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.UnspecifiedInstrumentCompartmentWithLock);

            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_AuthorizedSampleDrawer_ShouldCreateAuditTrailWithCorrectEventId()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1);

            await handler.Handle(command, TestContext.Current.CancellationToken);

            _senderMock.Verify(x => x.Send(
                It.Is<CreateAuditTrailRecordCommand>(cmd =>
                    cmd.EventId == AuditTrailEventId.EventId.SampleDrawerUnlock),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthorizedSampleDrawer_ShouldCreateAuditTrailWithCorrectEventDetails()
        {
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);
            var dateTime = DateTimeOffset.UtcNow;

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);
            _dateTimeMock.Setup(x => x.UtcNow).Returns(dateTime);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1);

            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
            _senderMock.Verify(x => x.Send(
               It.Is<CreateAuditTrailRecordCommand>(cmd =>
               cmd.EventId == AuditTrailEventId.EventId.SampleDrawerUnauthorizedUnlockAttempt),
               It.IsAny<CancellationToken>()), Times.Once);
        }

        [Trait("ProductRequirement", "PR_2164")]
        [Trait("ModuleRequirement", "MR_5512")]
        [Fact]
        public async Task Handle_AuthorizedSolidWasteDrawer_UnlocksCompartment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer);

            // Act
            await handler.Handle(command, TestContext.Current.CancellationToken);

            // Assert
            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer), It.IsAny<CancellationToken>()), Times.Once);
            _senderMock.Verify(s => s.Send(It.IsAny<CompartmentLockStateChangedCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthorizedSolidWasteDrawer_ThrowsNotAllowedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer);

            // Act & Assert
            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_AuthorizedSampleDrawer2_UnlocksCompartment()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2);

            // Act
            await handler.Handle(command, TestContext.Current.CancellationToken);

            // Assert
            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthorizedSampleDrawer2_ThrowsNotAllowedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2);

            // Act & Assert
            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_AuthorizedSampleDrawer2_ShouldCreateAuditTrail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2);

            // Act
            await handler.Handle(command, TestContext.Current.CancellationToken);

            // Assert
            _senderMock.Verify(x => x.Send(
                It.Is<CreateAuditTrailRecordCommand>(cmd =>
                    cmd.EventId == AuditTrailEventId.EventId.SampleDrawerUnlock),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthorizedSampleDrawer2_ShouldCreateUnauthorizedAuditTrail()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageSamples)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2);

            // Act & Assert
            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
            _senderMock.Verify(x => x.Send(
               It.Is<CreateAuditTrailRecordCommand>(cmd =>
               cmd.EventId == AuditTrailEventId.EventId.SampleDrawerUnauthorizedUnlockAttempt),
               It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithRequestUserId_UsesRequestUserIdWhenCurrentUserServiceUserIdIsNull()
        {
            // Arrange
            var requestUserId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns((Guid?)null);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(requestUserId, Permissions.ManageReagents)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer, requestUserId);

            // Act
            await handler.Handle(command, TestContext.Current.CancellationToken);

            // Assert
            Mock.Get(session.Connection).Verify(c =>
                c.UnlockCompartmentAsync(It.Is<UnlockCompartmentRequest>(r => r.Compartment == EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer), It.IsAny<CancellationToken>()), Times.Once);
            _authServiceMock.Verify(x => x.AuthorizeUserAsync(requestUserId, Permissions.ManageReagents), Times.Once);
        }

        [Fact]
        public async Task Handle_AuthorizedCompartment_SendsCompartmentLockStateChangedCommand()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var compartment = EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer;
            var instrumentId = 42;
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(instrumentId)).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, compartment);

            // Act
            await handler.Handle(command, TestContext.Current.CancellationToken);

            // Assert
            _senderMock.Verify(s => s.Send(
                It.Is<CompartmentLockStateChangedCommand>(c =>
                    c.InstrumentNumber == instrument.SerialNumber &&
                    c.Compartment == compartment),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UnauthorizedCompartment_DoesNotSendCompartmentLockStateChangedCommand()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            // Act & Assert
            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
            _senderMock.Verify(s => s.Send(It.IsAny<CompartmentLockStateChangedCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnauthorizedHood_ThrowsNotAllowedException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument(state: InstrumentState.Standby);
            var session = CreateSession(instrument.SerialNumber);

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageInstruments)).ReturnsAsync(false);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentHood);

            // Act & Assert
            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Handle_UnlockCompartmentAsyncThrows_PropagatesException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var instrument = CreateInstrument();
            var connMock = new Mock<IInstrumentConnection>();
            connMock.Setup(c => c.UnlockCompartmentAsync(It.IsAny<UnlockCompartmentRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Connection lost"));

            var session = new CommunicationSession
            {
                InstrumentNumber = instrument.SerialNumber,
                Connection = connMock.Object
            };

            _currentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
            _instrumentManagerMock.Setup(x => x.GetInstrument(It.IsAny<string>())).Returns(instrument);
            _authServiceMock.Setup(x => x.AuthorizeUserAsync(userId, Permissions.ManageReagents)).ReturnsAsync(true);
            _sessionManagerMock.Setup(x => x.GetSession(instrument.SerialNumber)).Returns(session);

            var handler = CreateHandler();
            var command = new UnlockCompartmentCommand(instrument.SerialNumber, EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer);

            // Act & Assert
            await Assert.ThrowsAsync<NotAllowedException>(() => handler.Handle(command, TestContext.Current.CancellationToken));
        }
    }
}
