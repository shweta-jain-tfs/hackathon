using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Services;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class ApiKeyServiceTests
    {
        public class TheGetApiKeyMethod
        {
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly ApiKeyService _sut;

            public TheGetApiKeyMethod()
            {
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
                settingsSnapshotMock.Setup(s => s.Value).Returns(new LabCommunitySettings { LaboratoryCode = "123456" });

                var serviceProviderMock = new Mock<IServiceProvider>();
                serviceProviderMock.Setup(sp => sp.GetService(typeof(ILabCommunityDbContext))).Returns(_dbContextMock.Object);
                serviceProviderMock.Setup(sp => sp.GetService(typeof(IOptionsSnapshot<LabCommunitySettings>))).Returns(settingsSnapshotMock.Object);

                var scopeMock = new Mock<IServiceScope>();
                scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

                var scopeFactoryMock = new Mock<IServiceScopeFactory>();
                scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

                _sut = new ApiKeyService(scopeFactoryMock.Object);
            }

            [Fact]
            public async Task ReturnsNull_WhenNoActiveEnrollment()
            {
                // Arrange
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Enrollment)null!);

                // Act
                var result = await _sut.GetApiKey();

                // Assert
                Assert.Null(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Fact]
            public async Task ReturnsApiKey_WhenActiveEnrollmentExists()
            {
                // Arrange
                var enrollment = new Enrollment { ApiKey = "abc-123" };
                _dbContextMock.Setup(db => db.GetActiveEnrollmentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(enrollment);

                // Act
                var result = await _sut.GetApiKey();

                // Assert
                Assert.Equal("abc-123", result);
            }
        }
    }
}