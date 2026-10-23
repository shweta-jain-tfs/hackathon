using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.LabCommunity.Application.Services.GetConfiguration;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityConfigurationServiceTests
    {
        public class TheGetConfigurationMethod
        {
            private readonly Mock<ILabCommunityApiClient> _clientMock;
            private readonly Mock<ILogger<LabCommunityConfigurationService>> _loggerMock;
            private readonly LabCommunityConfigurationService _sut;

            public TheGetConfigurationMethod()
            {
                _clientMock = new Mock<ILabCommunityApiClient>();
                _loggerMock = new Mock<ILogger<LabCommunityConfigurationService>>();

                _sut = new LabCommunityConfigurationService(_clientMock.Object, _loggerMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task CallsClientConfigurationAndReturnsSchedulingConfiguration()
            {
                // Arrange
                _clientMock.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new ConfigurationResponse
                    {
                        CheckIn = new CheckInConfiguration { IntervalSeconds = 120 },
                        SystemStatus = new SystemStatusConfiguration
                        {
                            Enabled = true,
                            IntervalSeconds = 300
                        },
                        ReportInstrumentErrors = new ReportInstrumentErrorsConfiguration { Enabled = true },
                        ReportCalibrationAndControlResults = new ReportCalibrationAndControlResultsConfiguration { Enabled = true }
                    });

                // Act
                var configuration = await _sut.GetConfiguration(TestContext.Current.CancellationToken);

                // Assert
                _clientMock.Verify(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Once);
                Assert.Equal(TimeSpan.FromSeconds(120), configuration!.CheckInInterval);
                Assert.True(configuration.SystemStatusEnabled);
                Assert.Equal(TimeSpan.FromSeconds(300), configuration.SystemStatusInterval);
                Assert.True(configuration.ReportInstrumentErrorsEnabled);
                Assert.True(configuration.ReportCalibrationAndQualityResultsEnabled);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task LogsWarningAndReturnsNull_WhenApiExceptionOccurs()
            {
                // Arrange
                var apiException = new ApiException(
                    "error",
                    500,
                    "server error",
                    new Dictionary<string, IEnumerable<string>>(),
                    null);

                _clientMock.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(apiException);

                // Act
                var configuration = await _sut.GetConfiguration(TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(configuration);
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
            [Trait("ModuleRequirement", "MR_5989")]
            [Fact]
            public async Task LogsErrorAndReturnsNull_WhenUnexpectedExceptionOccurs()
            {
                // Arrange
                var unexpectedException = new HttpRequestException("Unexpected error");
                _clientMock.Setup(c => c.GetConfigurationAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(unexpectedException);

                // Act
                var configuration = await _sut.GetConfiguration(TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(configuration);
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