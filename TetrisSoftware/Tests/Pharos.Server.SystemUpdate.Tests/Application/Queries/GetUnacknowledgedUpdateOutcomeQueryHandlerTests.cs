using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Application.Queries.GetUnacknowledgedUpdateOutcome;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Tests.Application.Queries;

public static class GetUnacknowledgedUpdateOutcomeQueryHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<ISystemUpdateDbContext> _dbContext;
        private readonly Mock<IUpdateOutcomeEvaluator> _updateEvaluator;
        private readonly GetUnacknowledgedUpdateOutcomeQueryHandler _sut;

        public TheHandleMethod()
        {
            _dbContext = new Mock<ISystemUpdateDbContext>();
            _updateEvaluator = new Mock<IUpdateOutcomeEvaluator>();

            _sut = new GetUnacknowledgedUpdateOutcomeQueryHandler(_dbContext.Object, _updateEvaluator.Object, new Mock<ILogger<GetUnacknowledgedUpdateOutcomeQueryHandler>>().Object);
        }

        [Fact]
        public async Task EvaluatesPendingUpdateOutcome_BeforeFetchingUnacknowledgedUpdate()
        {
            // Act
            await _sut.Handle(new GetUnacknowledgedUpdateOutcomeQuery(), TestContext.Current.CancellationToken);

            // Assert
            _updateEvaluator.Verify(e => e.EvaluateUpdateOutcome(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ReturnsAcknowledged_WhenNoPreviousUpdates()
        {
            // Arrange
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((SystemUpdateSession?)null);

            // Act
            var result = await _sut.Handle(new GetUnacknowledgedUpdateOutcomeQuery(), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.IsAcknowledged);
        }

        [Fact]
        public async Task ReturnsAcknowledgedFalse_WhenLatestUpdateNotAcknowledged()
        {
            // Arrange
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SystemUpdateSession());

            // Act
            var result = await _sut.Handle(new GetUnacknowledgedUpdateOutcomeQuery(), TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result.IsAcknowledged);
        }

        [Fact]
        public async Task ReturnsSuccessful_WhenLatestUpdateSuccessful()
        {
            // Arrange
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SystemUpdateSession
                {
                    Status = UpdateStatus.Successful
                });

            // Act
            var result = await _sut.Handle(new GetUnacknowledgedUpdateOutcomeQuery(), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public async Task ReturnsSuccessfulFalse_WhenLatestUpdateFailed()
        {
            // Arrange
            _dbContext.Setup(db => db.GetLatestUnacknowledgedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SystemUpdateSession
                {
                    Status = UpdateStatus.Failed
                });

            // Act
            var result = await _sut.Handle(new GetUnacknowledgedUpdateOutcomeQuery(), TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result.IsSuccessful);
        }
    }
}
