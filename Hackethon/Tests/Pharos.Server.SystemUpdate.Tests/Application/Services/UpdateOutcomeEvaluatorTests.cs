using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Application.Services;

namespace Pharos.Server.SystemUpdate.Tests.Application.Services;

public static class UpdateOutcomeEvaluatorTests
{
    public class TheEvaluateUpdateOutcomeMethod
    {
        private readonly UpdateOutcomeEvaluator _sut;
        private readonly Mock<ISystemUpdateDbContext> _dbContext;
        private readonly Mock<IPharosVersionService> _versionService;
        private readonly Mock<ISender> _sender;

        public TheEvaluateUpdateOutcomeMethod()
        {
            _dbContext = new Mock<ISystemUpdateDbContext>();
            _versionService = new Mock<IPharosVersionService>();
            _sender = new Mock<ISender>();

            _sut = new UpdateOutcomeEvaluator(_dbContext.Object,
                _versionService.Object,
                new Mock<ILogger<UpdateOutcomeEvaluator>>().Object);
        }

        [Fact]
        public async Task DoesNothing_WhenNoPendingUpdate()
        {
            // Arrange
            _dbContext.Setup(db => db.GetLatestPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.SystemUpdateSession?)null);

            // Act
            await _sut.EvaluateUpdateOutcome(TestContext.Current.CancellationToken);

            // Assert
            _dbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            _sender.Verify(s => s.Send(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MarksUpdateAsSuccessful_WhenVersionIncreased()
        {
            // Arrange
            var pendingUpdate = new Domain.SystemUpdateSession
            {
                PreviousVersion = "1.0.0",
                Status = Domain.UpdateStatus.Pending
            };

            _dbContext.Setup(db => db.GetLatestPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingUpdate);
            _versionService.Setup(v => v.Version).Returns("1.1.0");

            // Act
            await _sut.EvaluateUpdateOutcome(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(Domain.UpdateStatus.Successful, pendingUpdate.Status);
            _dbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MarksUpdateAsFailed_WhenVersionNotIncreased()
        {
            // Arrange
            var pendingUpdate = new Domain.SystemUpdateSession
            {
                PreviousVersion = "1.0.0",
                Status = Domain.UpdateStatus.Pending
            };

            _dbContext.Setup(db => db.GetLatestPendingAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(pendingUpdate);
            _versionService.Setup(v => v.Version).Returns("1.0.0");

            // Act
            await _sut.EvaluateUpdateOutcome(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(Domain.UpdateStatus.Failed, pendingUpdate.Status);
            _dbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
