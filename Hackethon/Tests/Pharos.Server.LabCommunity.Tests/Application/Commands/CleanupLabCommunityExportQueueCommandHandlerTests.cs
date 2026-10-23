using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands
{
    public static class CleanupLabCommunityExportQueueCommandHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<IDateTime> _dateTimeMock;
            private readonly CleanupLabCommunityExportQueueCommandHandler _sut;

            public TheHandleMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _dateTimeMock = new Mock<IDateTime>();
                _sut = new CleanupLabCommunityExportQueueCommandHandler(
                    _dbContextMock.Object,
                    _dateTimeMock.Object,
                    Mock.Of<ILogger<CleanupLabCommunityExportQueueCommandHandler>>());
            }

            [Fact]
            public async Task DeletesExportItemsOlderThanConfiguredRetention()
            {
                // Arrange
                var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
                _dateTimeMock.Setup(dateTime => dateTime.UtcNow).Returns(now);

                // Act
                await _sut.Handle(
                    new CleanupLabCommunityExportQueueCommand(90),
                    TestContext.Current.CancellationToken);

                // Assert
                _dbContextMock.Verify(
                    dbContext => dbContext.DeleteExportItemsOlderThanAsync(
                        now.AddDays(-90),
                        TestContext.Current.CancellationToken),
                    Times.Once);
            }
        }
    }
}