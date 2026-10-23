using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Infrastructure;
using Pharos.Server.SystemUpdate.Application.Commands;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Commands;

public static class AcknowledgeUpdateCommandHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<ISystemUpdateDbContext> _dbContext;
        private readonly AcknowledgeUpdateCommandHandler _sut;
        private readonly Mock<ISystemUpdateHub> _hub;
        private readonly Mock<IDateTime> _time;

        public TheHandleMethod()
        {
            _dbContext = new Mock<ISystemUpdateDbContext>();
            _hub = new Mock<ISystemUpdateHub>();
            _time = new Mock<IDateTime>();

            _sut = new AcknowledgeUpdateCommandHandler(_dbContext.Object, _hub.Object, _time.Object);
        }

        [Fact]
        public async Task DoesNothing_WhenNoUnacknowledgedUpdates()
        {
            // Arrange
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((SystemUpdateSession?)null);

            // Act
            await _sut.Handle(new AcknowledgeUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _dbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _hub.Verify(hub => hub.UpdateOutcomeAcknowledged(), Times.Never);
        }

        [Fact]
        public async Task AcknowledgesUpdate_WhenUnacknowledgedUpdateExists()
        {
            // Arrange
            var session = new SystemUpdateSession { Id = Guid.NewGuid() };
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var now = DateTimeOffset.UtcNow;
            _time.Setup(t => t.UtcNow).Returns(now);

            // Act
            await _sut.Handle(new AcknowledgeUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _dbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal(now, session.AcknowledgedAtUtc);
        }

        [Fact]
        public async Task NotifiesClients_WhenUnacknowledgedUpdateExists()
        {
            // Arrange
            var session = new SystemUpdateSession { Id = Guid.NewGuid() };
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            // Act
            await _sut.Handle(new AcknowledgeUpdateCommand(), TestContext.Current.CancellationToken);

            // Assert
            _hub.Verify(hub => hub.UpdateOutcomeAcknowledged(), Times.Once);
        }
    }
}
