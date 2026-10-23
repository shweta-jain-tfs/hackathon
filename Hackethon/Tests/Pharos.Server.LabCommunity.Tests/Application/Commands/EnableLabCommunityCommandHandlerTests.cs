using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.EnrollmentApi;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands
{
    public static class EnableLabCommunityCommandHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<ILabCommunityEnrollmentService> _enrollmentServiceMock;
            private readonly Mock<ILabCommunityConnectionHub> _connectionHubMock;
            private readonly EnableLabCommunityCommandHandler _sut;
            private readonly LabCommunitySettings _settings;

            public TheHandleMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _enrollmentServiceMock = new Mock<ILabCommunityEnrollmentService>();
                _connectionHubMock = new Mock<ILabCommunityConnectionHub>();
                _connectionHubMock.Setup(h => h.LabCommunityStatusChanged()).Returns(Task.CompletedTask);
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
                var loggerMock = new Mock<ILogger<EnableLabCommunityCommandHandler>>();

                _sut = new EnableLabCommunityCommandHandler(
                    _dbContextMock.Object,
                    _enrollmentServiceMock.Object,
                    settingsSnapshotMock.Object,
                    _connectionHubMock.Object,
                    loggerMock.Object);

                // Default setup
                _settings = new LabCommunitySettings
                {
                    LaboratoryCode = "123456"
                };

                settingsSnapshotMock.Setup(s => s.Value).Returns(_settings);

                _dbContextMock.Setup(db => db.Enrollments).Returns(new Mock<DbSet<Enrollment>>().Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task AttemptsEnrollmentWithExistingDeviceId_WhenActiveEnrollmentExists()
            {
                // Arrange
                var existingEnrollment = new Enrollment
                {
                    LaboratoryCode = _settings.LaboratoryCode,
                    DeviceId = "existing-device-id",
                    ApiKey = "existing-api-key"
                };

                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingEnrollment);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, existingEnrollment.DeviceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(LabCommunityEnrollmentResult.AlreadyEnrolled());

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _enrollmentServiceMock.Verify(
                    es => es.EnrollPharos(_settings, existingEnrollment.DeviceId, It.IsAny<CancellationToken>()),
                    Times.Once);
                _dbContextMock.Verify(db => db.Enrollments.Add(It.IsAny<Enrollment>()), Times.Never);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task MarksExistingEnrollmentCredentialsInvalidAndNotifiesClient_WhenCredentialsAreInvalid()
            {
                // Arrange
                var existingEnrollment = new Enrollment
                {
                    LaboratoryCode = _settings.LaboratoryCode,
                    DeviceId = "existing-device-id",
                    ApiKey = "existing-api-key",
                    ValidCredentials = true
                };

                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingEnrollment);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, existingEnrollment.DeviceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(LabCommunityEnrollmentResult.InvalidCredentials());

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                Assert.False(existingEnrollment.ValidCredentials);
                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task MarksExistingEnrollmentCredentialsValidAndNotifiesClient_WhenDeviceIsAlreadyEnrolled()
            {
                // Arrange
                var existingEnrollment = new Enrollment
                {
                    LaboratoryCode = _settings.LaboratoryCode,
                    DeviceId = "existing-device-id",
                    ApiKey = "existing-api-key",
                    ValidCredentials = false
                };

                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingEnrollment);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, existingEnrollment.DeviceId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(LabCommunityEnrollmentResult.AlreadyEnrolled());

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                Assert.True(existingEnrollment.ValidCredentials);
                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task FailsEnrollment_WhenApiKeyNotReturned()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(LabCommunityEnrollmentResult.Failed());

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _dbContextMock.Verify(db => db.Enrollments.Add(It.IsAny<Enrollment>()), Times.Never);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task SuccessfullyEnrolls_WhenNoActiveEnrollmentExistsAndApiKeyReturned()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(LabCommunityEnrollmentResult.Enrolled("test-api-key"));

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _dbContextMock.Verify(db => db.Enrollments.Add(It.Is<Enrollment>(e =>
                    e.LaboratoryCode == _settings.LaboratoryCode &&
                    e.ApiKey == "test-api-key" &&
                    e.ValidCredentials)), Times.Once);

                _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
                _connectionHubMock.Verify(h => h.LabCommunityStatusChanged(), Times.Once);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task SendsAndStoresSameDeviceId_WhenEnrollmentSucceeds()
            {
                // Arrange
                string? sentDeviceId = null;
                Enrollment? storedEnrollment = null;

                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<LabCommunitySettings, string, CancellationToken>((_, deviceId, _) => sentDeviceId = deviceId)
                    .ReturnsAsync(LabCommunityEnrollmentResult.Enrolled("test-api-key"));
                _dbContextMock.Setup(db => db.Enrollments.Add(It.IsAny<Enrollment>()))
                    .Callback<Enrollment>(enrollment => storedEnrollment = enrollment);

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                Assert.False(string.IsNullOrWhiteSpace(sentDeviceId));
                Assert.Equal(sentDeviceId, storedEnrollment?.DeviceId);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Fact]
            public async Task DoesNotStoreApiKey_WhenApiKeyIsEmpty()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(LabCommunityEnrollmentResult.Enrolled(string.Empty));

                // Act
                await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _dbContextMock.Verify(db => db.Enrollments.Add(It.IsAny<Enrollment>()), Times.Never);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_4049")]
            [Theory]
            [InlineData(LabCommunityEnrollmentStatus.Failed, false)]
            [InlineData(LabCommunityEnrollmentStatus.AlreadyEnrolled, true)]
            [InlineData(LabCommunityEnrollmentStatus.Enrolled, true)]
            [InlineData(LabCommunityEnrollmentStatus.InvalidCredentials, false)]
            public async Task ReturnsCorrectBoolean_BasedOnEnrollmentStatus(LabCommunityEnrollmentStatus status, bool expected)
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);
                _enrollmentServiceMock.Setup(es => es.EnrollPharos(_settings, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new LabCommunityEnrollmentResult(status, "test-api-key"));

                // Act
                var result = await _sut.Handle(new EnableLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(expected, result);
            }
        }
    }
}