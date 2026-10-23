using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.LabCommunity.Application.Interfaces.InstrumentStatus;
using Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;
using Xunit;
using ApiInstrument = LabCommunity.API.Client.Instrument;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityInstrumentStatusServiceTests
    {
        public class TheSendStatusMethod
        {
            private readonly Mock<ILabCommunityApiClient> _clientMock;
            private readonly Mock<IInstrumentStatusSnapshotProvider> _snapshotProviderMock;
            private readonly Mock<ILogger<LabCommunityInstrumentStatusService>> _loggerMock;
            private readonly LabCommunityInstrumentStatusService _sut;

            public TheSendStatusMethod()
            {
                _clientMock = new Mock<ILabCommunityApiClient>();
                _snapshotProviderMock = new Mock<IInstrumentStatusSnapshotProvider>();
                _loggerMock = new Mock<ILogger<LabCommunityInstrumentStatusService>>();
                _sut = new LabCommunityInstrumentStatusService(
                    _clientMock.Object,
                    _snapshotProviderMock.Object,
                    _loggerMock.Object);
            }

            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public async Task SendsCurrentSnapshotToGeneratedClientInstrumentStatusEndpoint()
            {
                // Arrange
                var request = new InstrumentStatusRequest
                {
                    ReportedAt = new DateTimeOffset(2026, 6, 29, 9, 0, 0, TimeSpan.Zero),
                    Instruments =
                    [
                        new ApiInstrument
                        {
                            Type = InstrumentType.E500,
                            SerialNumber = "E500-123",
                            Software = new InstrumentSoftwareStatus
                            {
                                State = "Standby"
                            }
                        }
                    ]
                };
                _snapshotProviderMock.Setup(p => p.CreateSnapshot()).Returns(request);

                // Act
                var result = await _sut.SendStatusAsync(TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result);
                _snapshotProviderMock.Verify(p => p.CreateSnapshot(), Times.Once);
                _clientMock.Verify(
                    client => client.UpsertInstrumentStatusesAsync(request, TestContext.Current.CancellationToken),
                    Times.Once);
            }

            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public async Task SkipsClientCall_WhenSnapshotHasNoInstruments()
            {
                // Arrange
                _snapshotProviderMock.Setup(p => p.CreateSnapshot())
                    .Returns(new InstrumentStatusRequest
                    {
                        ReportedAt = new DateTimeOffset(2026, 6, 29, 9, 0, 0, TimeSpan.Zero),
                        Instruments = []
                    });

                // Act
                var result = await _sut.SendStatusAsync(TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result);
                _clientMock.Verify(
                    client => client.UpsertInstrumentStatusesAsync(
                        It.IsAny<InstrumentStatusRequest>(),
                        It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6018")]
            [Trait("ModuleRequirement", "MR_6051")]
            [Fact]
            public async Task LogsWarningAndReturnsFalse_WhenApiExceptionOccurs()
            {
                // Arrange
                var request = new InstrumentStatusRequest
                {
                    ReportedAt = new DateTimeOffset(2026, 6, 29, 9, 0, 0, TimeSpan.Zero),
                    Instruments =
                    [
                        new ApiInstrument
                        {
                            Type = InstrumentType.E500,
                            SerialNumber = "E500-123",
                            Software = new InstrumentSoftwareStatus
                            {
                                State = "Standby"
                            }
                        }
                    ]
                };
                _snapshotProviderMock.Setup(p => p.CreateSnapshot()).Returns(request);

                var apiException = new ApiException(
                    "error",
                    500,
                    "server error",
                    new Dictionary<string, IEnumerable<string>>(),
                    null);
                _clientMock.Setup(c => c.UpsertInstrumentStatusesAsync(
                        It.IsAny<InstrumentStatusRequest>(),
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