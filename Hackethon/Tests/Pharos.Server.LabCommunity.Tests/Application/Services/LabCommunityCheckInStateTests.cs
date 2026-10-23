using Pharos.Server.LabCommunity.Application.Services.CheckIn;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class LabCommunityCheckInStateTests
    {
        public class TheHasRecentSuccessfulCheckInMethod
        {
            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public void ReturnsTrue_WhenLatestAttemptSucceededWithinStaleThreshold()
            {
                // Arrange
                var sut = new LabCommunityCheckInState();
                var now = new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero);

                sut.MarkSucceeded(now.AddMinutes(-1));

                // Act
                var result = sut.HasRecentSuccessfulCheckIn(now, TimeSpan.FromMinutes(4));

                // Assert
                Assert.True(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public void ReturnsFalse_WhenLatestAttemptFailed()
            {
                // Arrange
                var sut = new LabCommunityCheckInState();
                var now = new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero);

                sut.MarkSucceeded(now.AddMinutes(-1));

                // Act
                sut.MarkFailed(now);
                var result = sut.HasRecentSuccessfulCheckIn(now, TimeSpan.FromMinutes(4));

                // Assert
                Assert.False(result);
            }

            [Trait("ProductRequirement", "PR_533")]
            [Trait("ModuleRequirement", "MR_5811")]
            [Trait("ModuleRequirement", "MR_5825")]
            [Fact]
            public void ReturnsFalse_WhenLatestSuccessfulAttemptIsStale()
            {
                // Arrange
                var sut = new LabCommunityCheckInState();
                var now = new DateTimeOffset(2026, 5, 5, 12, 0, 0, TimeSpan.Zero);
                sut.MarkSucceeded(now.AddMinutes(-10));

                // Act
                var result = sut.HasRecentSuccessfulCheckIn(now, TimeSpan.FromMinutes(4));

                // Assert
                Assert.False(result);
            }
        }
    }
}