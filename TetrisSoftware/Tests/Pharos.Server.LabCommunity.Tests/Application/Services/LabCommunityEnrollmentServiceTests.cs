using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Services.EnrollmentApi;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityEnrollmentServiceTests
    {
        public class TheEnrollAsyncMethod
        {
            private const string _deviceId = "pharos-device-1";
            private readonly Mock<ILabCommunityApiClient> _clientMock;
            private readonly LabCommunityEnrollmentService _sut;
            private readonly LabCommunitySettings _settings;

            public TheEnrollAsyncMethod()
            {
                _clientMock = new Mock<ILabCommunityApiClient>();
                var loggerMock = new Mock<ILogger<LabCommunityEnrollmentService>>();

                _settings = new LabCommunitySettings { LaboratoryCode = "123", Username = "user", Password = "pass" };

                _sut = new LabCommunityEnrollmentService(_clientMock.Object, loggerMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task ReturnsEnrolledResult_WhenEnrollmentSucceeds()
            {
                // Arrange
                var response = new EnrollmentResponse { ApiKey = "api-key" };

                _clientMock.Setup(c => c.EnrollAsync(It.IsAny<EnrollmentRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(response);

                // Act
                var result = await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(LabCommunityEnrollmentStatus.Enrolled, result.Status);
                Assert.Equal("api-key", result.ApiKey);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task EnrollsWithSettingsValues()
            {
                // Act
                await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                _clientMock.Verify(c => c.EnrollAsync(
                    It.Is<EnrollmentRequest>(r =>
                        r.LaboratoryCode == _settings.LaboratoryCode &&
                        r.Username == _settings.Username &&
                        r.Password == _settings.Password),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task EnrollsWithDeviceId()
            {
                // Act
                await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                _clientMock.Verify(c => c.EnrollAsync(
                    It.Is<EnrollmentRequest>(r => r.DeviceId == _deviceId),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task EnrollsWithPharosAsProductType()
            {
                // Act
                await _sut.EnrollPharos(_settings, _deviceId, CancellationToken.None);

                // Assert
                _clientMock.Verify(c => c.EnrollAsync(
                    It.Is<EnrollmentRequest>(r => r.Product == OperatorSoftwareProduct.Pharos),
                    It.IsAny<CancellationToken>()), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task ReturnsInvalidCredentialsResult_OnUnauthorizedApiException()
            {
                // Arrange
                var apiException = new ApiException(
                    "error",
                    401,
                    "Invalid credentials",
                    new Dictionary<string, IEnumerable<string>>(),
                    null);

                _clientMock.Setup(c => c.EnrollAsync(It.IsAny<EnrollmentRequest>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(apiException);

                // Act
                var result = await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(LabCommunityEnrollmentStatus.InvalidCredentials, result.Status);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task ReturnsAlreadyEnrolledResult_OnDeviceAlreadyEnrolledApiException()
            {
                // Arrange
                var apiException = new ApiException(
                    "error",
                    409,
                    "Device already enrolled",
                    new Dictionary<string, IEnumerable<string>>(),
                    null);

                _clientMock.Setup(c => c.EnrollAsync(It.IsAny<EnrollmentRequest>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(apiException);

                // Act
                var result = await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(LabCommunityEnrollmentStatus.AlreadyEnrolled, result.Status);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task ReturnsFailedResult_OnApiException()
            {
                // Arrange
                var apiException = new ApiException(
                    "error",
                    500,
                    "server error",
                    new Dictionary<string, IEnumerable<string>>(),
                    null);

                _clientMock.Setup(c => c.EnrollAsync(It.IsAny<EnrollmentRequest>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(apiException);

                // Act
                var result = await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(LabCommunityEnrollmentStatus.Failed, result.Status);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task ReturnsFailedResult_OnGenericException()
            {
                // Arrange
                _clientMock.Setup(c => c.EnrollAsync(It.IsAny<EnrollmentRequest>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("boom"));

                // Act
                var result = await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(LabCommunityEnrollmentStatus.Failed, result.Status);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task ReturnsFailedResult_WhenEnrollmentSucceedsWithEmptyApiKey()
            {
                // Arrange
                var response = new EnrollmentResponse { ApiKey = "" };

                _clientMock.Setup(c => c.EnrollAsync(It.IsAny<EnrollmentRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(response);

                // Act
                var result = await _sut.EnrollPharos(_settings, _deviceId, TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(LabCommunityEnrollmentStatus.Failed, result.Status);
            }
        }
    }
}