using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Queries;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Queries
{
    public static class IsLabCommunityEnabledQueryHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly LabCommunitySettings _settings;
            private readonly IsLabCommunityEnabledQueryHandler _sut;

            public TheHandleMethod()
            {
                var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
                _settings = new LabCommunitySettings();

                settingsSnapshotMock.Setup(s => s.Value).Returns(_settings);
                _sut = new IsLabCommunityEnabledQueryHandler(settingsSnapshotMock.Object);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5791")]
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ReturnsEnableSetting(bool isEnabled)
            {
                // Arrange
                _settings.Enable = isEnabled;

                // Act
                var result = await _sut.Handle(new IsLabCommunityEnabledQuery(), TestContext.Current.CancellationToken);

                // Assert
                Assert.Equal(isEnabled, result);
            }
        }
    }
}