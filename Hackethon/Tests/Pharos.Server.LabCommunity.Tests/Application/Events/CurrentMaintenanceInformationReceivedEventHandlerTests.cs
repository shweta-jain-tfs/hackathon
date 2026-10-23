using Google.Protobuf.WellKnownTypes;
using Pharos.Server.Application.Instrument;
using Pharos.Server.LabCommunity.Application.Events;
using Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;
using Xunit;
using LabCommunityMaintenanceInformation = LabCommunity.API.Client.MaintenanceActivity;

namespace Pharos.Server.LabCommunity.Tests.Application.Events
{
    public static class CurrentMaintenanceInformationReceivedEventHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly InstrumentMaintenanceActivityCache _cache;
            private readonly CurrentMaintenanceInformationReceivedEventHandler _sut;

            public TheHandleMethod()
            {
                _cache = new InstrumentMaintenanceActivityCache();
                _sut = new CurrentMaintenanceInformationReceivedEventHandler(_cache);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public async Task CachesReportedMaintenanceInformation()
            {
                // Arrange
                var lastStarted = new DateTimeOffset(2026, 8, 10, 7, 0, 0, TimeSpan.Zero);
                var lastEnded = new DateTimeOffset(2026, 8, 10, 7, 30, 0, TimeSpan.Zero);
                var notification = new CurrentMaintenanceInformationReceivedEvent(
                    "E500-123",
                    new CurrentMaintenanceInformation
                    {
                        MaintenanceInformation =
                        {
                            new MaintenanceInformation
                            {
                                Type = EnumMaintenance.DailyRinse,
                                Status = EnumMaintenanceStatus.MaintenanceStatusOk,
                                LastStarted = Timestamp.FromDateTimeOffset(lastStarted),
                                LastEnded = Timestamp.FromDateTimeOffset(lastEnded)
                            }
                        },
                        Rinsed = true,
                        MaintenanceNeeded = false
                    });

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                var cached = Assert.IsType<LabCommunityMaintenanceInformation>(
                    _cache.Get("E500-123"));
                var activity = Assert.Single(cached.MaintenanceActivities);
                Assert.Equal("DailyRinse", activity.Type);
                Assert.Equal("MaintenanceStatusOk", activity.Status);
                Assert.Equal(lastStarted, activity.LastStarted);
                Assert.Equal(lastEnded, activity.LastEnded);
                Assert.True(cached.Rinsed);
                Assert.False(cached.MaintenanceNeeded);
            }
        }
    }
}