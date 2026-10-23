using LabCommunity.API.Client;
using Pharos.Server.Application.Instrument;
using Pharos.Server.LabCommunity.Application.Events;
using Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Events
{
    public static class InstrumentDeletedEventHandlerTests
    {
        public class TheHandleMethod
        {
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public async Task RemovesCachedMaintenanceInformation()
            {
                // Arrange
                var cache = new InstrumentMaintenanceActivityCache();
                cache.Set(
                    "E500-123",
                    new MaintenanceActivity
                    {
                        MaintenanceActivities = [],
                        Rinsed = true,
                        MaintenanceNeeded = false
                    });
                var sut = new InstrumentDeletedEventHandler(cache);

                // Act
                await sut.Handle(new InstrumentDeletedEvent("E500-123", 42), TestContext.Current.CancellationToken);

                // Assert
                Assert.Null(cache.Get("E500-123"));
            }
        }
    }
}