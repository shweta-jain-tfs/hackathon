using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Interfaces.PharosStatus;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands
{
    public static class SendPharosStatusToLabCommunityCommandHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<ILabCommunityPharosStatusService> _statusServiceMock;
            private readonly SendPharosStatusToLabCommunityCommandHandler _sut;
            private readonly LabCommunitySettings _settings;

            public TheHandleMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _statusServiceMock = new Mock<ILabCommunityPharosStatusService>();
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();

                _settings = new LabCommunitySettings
                {
                    LaboratoryCode = "123456"
                };

                settingsSnapshotMock.Setup(s => s.Value).Returns(_settings);
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Enrollment());
                _statusServiceMock.Setup(s => s.SendStatusAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                _sut = new SendPharosStatusToLabCommunityCommandHandler(
                    _dbContextMock.Object,
                    _statusServiceMock.Object,
                    settingsSnapshotMock.Object,
                    Mock.Of<ILogger<SendPharosStatusToLabCommunityCommandHandler>>());
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Fact]
            public async Task SkipsStatusUpload_WhenNoActiveEnrollmentExists()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);

                // Act
                await _sut.Handle(new SendPharosStatusToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _statusServiceMock.Verify(
                    s => s.SendStatusAsync(It.IsAny<CancellationToken>()),
                    Times.Never);
            }

            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_6001")]
            [Fact]
            public async Task SendsStatus_WhenLabCommunityIsEnabledAndActiveEnrollmentExists()
            {
                // Act
                await _sut.Handle(new SendPharosStatusToLabCommunityCommand(), TestContext.Current.CancellationToken);

                // Assert
                _statusServiceMock.Verify(
                    s => s.SendStatusAsync(TestContext.Current.CancellationToken),
                    Times.Once);
            }
        }
    }
}