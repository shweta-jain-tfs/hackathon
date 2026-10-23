using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;
using Pharos.Server.LabCommunity.Application.Services.PharosStatus;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityPharosStatusServiceTests
    {
        public class TheSendStatusMethod
        {
            private readonly Mock<ILabCommunityApiClient> _clientMock;
            private readonly Mock<IPharosStatusSnapshotProvider> _snapshotProviderMock;
            private readonly Mock<ILogger<LabCommunityPharosStatusService>> _loggerMock;
            private readonly OperatorSoftwareStatusRequest _request;
            private readonly LabCommunityPharosStatusService _sut;

            public TheSendStatusMethod()
            {
                _clientMock = new Mock<ILabCommunityApiClient>();
                _snapshotProviderMock = new Mock<IPharosStatusSnapshotProvider>();
                _loggerMock = new Mock<ILogger<LabCommunityPharosStatusService>>();
                _request = new OperatorSoftwareStatusRequest
                {
                    ReportedAt = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero),
                    Software = new OperatorSoftwareStatus
                    {
                        Version = "1.4.0"
                    }
                };
                _snapshotProviderMock.Setup(p => p.CreateSnapshot()).Returns(_request);
                _sut = new LabCommunityPharosStatusService(
                    _clientMock.Object,
                    _snapshotProviderMock.Object,
                    _loggerMock.Object);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Trait("ModuleRequirement", "MR_6009")]
            [Fact]
            public async Task SendsCurrentSnapshotToGeneratedClientStatusEndpoint()
            {
                // Act
                var result = await _sut.SendStatusAsync(TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result);
                _snapshotProviderMock.Verify(p => p.CreateSnapshot(), Times.Once);
                _clientMock.Verify(
                    client => client.UpdateStatusAsync(_request, TestContext.Current.CancellationToken),
                    Times.Once);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_6001")]
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
                _clientMock.Setup(c => c.UpdateStatusAsync(
                        It.IsAny<OperatorSoftwareStatusRequest>(),
                        It.IsAny<CancellationToken>()))
                    .ThrowsAsync(apiException);

                // Act
                var result = await _sut.SendStatusAsync(TestContext.Current.CancellationToken);

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
        }
    }
}