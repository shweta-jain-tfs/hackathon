using Moq;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Queries;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Queries
{
    public static class GetLabCommunityConnectionStatusQueryHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityConnectionStatusService> _connectionStatusServiceMock;
            private readonly GetLabCommunityConnectionStatusQueryHandler _sut;

            public TheHandleMethod()
            {
                _connectionStatusServiceMock = new Mock<ILabCommunityConnectionStatusService>();

                _sut = new GetLabCommunityConnectionStatusQueryHandler(_connectionStatusServiceMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsConnected_WhenServiceReturnsTrue()
            {
                // Arrange
                _connectionStatusServiceMock.Setup(s => s.IsConnected(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

                // Act
                var result = await _sut.Handle(new GetLabCommunityConnectionStatusQuery(), TestContext.Current.CancellationToken);

                // Assert
                Assert.True(result.IsConnected);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Fact]
            public async Task ReturnsDisconnected_WhenServiceReturnsFalse()
            {
                // Arrange
                _connectionStatusServiceMock.Setup(s => s.IsConnected(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

                // Act
                var result = await _sut.Handle(new GetLabCommunityConnectionStatusQuery(), TestContext.Current.CancellationToken);

                // Assert
                Assert.False(result.IsConnected);
            }
        }
    }
}