using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.LabCommunity.Application.Services.CheckIn;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityCheckInServiceTests
    {
        public class TheCheckInMethod
        {
            private readonly Mock<ILabCommunityApiClient> _clientMock;
            private readonly Mock<ILogger<LabCommunityCheckInService>> _loggerMock;
            private readonly LabCommunityCheckInService _sut;

            public TheCheckInMethod()
            {
                _clientMock = new Mock<ILabCommunityApiClient>();
                _loggerMock = new Mock<ILogger<LabCommunityCheckInService>>();

                _sut = new LabCommunityCheckInService(_clientMock.Object, _loggerMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task CallsClientCheckIn()
            {
                // Arrange
                _clientMock.Setup(c => c.CheckInAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new CheckInResponse());

                // Act
                var result = await _sut.CheckIn(TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result);
                _clientMock.Verify(c => c.CheckInAsync(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task LogsWarningAndReturnsFalse_WhenApiExceptionOccurs()
            {
                // Arrange
                var apiException = new ApiException(
                    "error",
                    500,
                    "server error",
                    new Dictionary<string, IEnumerable<string>>(),
                    null);

                _clientMock.Setup(c => c.CheckInAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(apiException);

                // Act
                var result = await _sut.CheckIn(TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result);
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Warning,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception?>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public async Task LogsErrorAndReturnsFalse_WhenUnexpectedExceptionOccurs()
            {
                // Arrange
                var unexpectedException = new HttpRequestException("Unexpected error");
                _clientMock.Setup(c => c.CheckInAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(unexpectedException);

                // Act
                var result = await _sut.CheckIn(TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result);
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Error,
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception?>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
        }
    }
}