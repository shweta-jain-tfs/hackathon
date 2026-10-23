using Moq;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure;
using Pharos.Server.LabCommunity.Application.Services.PharosStatus;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class PharosStatusSnapshotProviderTests
    {
        public class TheCreateSnapshotMethod
        {
            [Trait("ProductRequirement", "PR_538")]
            [Trait("ModuleRequirement", "MR_6009")]
            [Fact]
            public void CreatesRunningSoftwareSnapshotWithVersionAndOperatingSystem()
            {
                // Arrange
                var versionServiceMock = new Mock<IPharosVersionService>();
                var dateTimeMock = new Mock<IDateTime>();
                var operatingSystemMock = new Mock<IOperatingSystemInformation>();
                var now = new DateTimeOffset(2026, 5, 25, 8, 0, 0, TimeSpan.Zero);
                var startedAt = new DateTimeOffset(2026, 5, 24, 6, 30, 0, TimeSpan.Zero);

                versionServiceMock.Setup(s => s.Version).Returns("1.4.0");
                versionServiceMock.Setup(s => s.StartedAt).Returns(startedAt);
                dateTimeMock.Setup(d => d.UtcNow).Returns(now);
                operatingSystemMock.Setup(o => o.Version).Returns("10.0.22631");
                operatingSystemMock.Setup(o => o.FriendlyName).Returns("Windows 11 Enterprise");

                var sut = new PharosStatusSnapshotProvider(
                    versionServiceMock.Object,
                    dateTimeMock.Object,
                    operatingSystemMock.Object);

                // Act
                var snapshot = sut.CreateSnapshot();

                // Assert
                Assert.Equal(now, snapshot.ReportedAt);
                Assert.Equal("1.4.0", snapshot.Software.Version);
                Assert.Equal(startedAt, snapshot.Software.RunningSince);
                Assert.Equal("10.0.22631", snapshot.OperatingSystem.Version);
                Assert.Equal("Windows 11 Enterprise", snapshot.OperatingSystem.FriendlyName);
            }
        }
    }
}