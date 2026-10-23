using FluentAssertions;
using LabCommunity.API.Client;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.LabCommunity.Application.Services.InstrumentStatus;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Services
{
    public static class InstrumentStatusSnapshotProviderTests
    {
        public class TheCreateSnapshotMethod
        {
            private readonly Mock<IInstrumentManager> _instrumentServiceMock;
            private readonly InstrumentMaintenanceActivityCache _maintenanceActivityCache;
            private readonly DateTimeOffset _now;
            private readonly InstrumentStatusSnapshotProvider _sut;

            public TheCreateSnapshotMethod()
            {
                _instrumentServiceMock = new Mock<IInstrumentManager>();
                _maintenanceActivityCache = new InstrumentMaintenanceActivityCache();
                var dateTimeMock = new Mock<IDateTime>();
                _now = new DateTimeOffset(2026, 6, 29, 9, 0, 0, TimeSpan.Zero);

                dateTimeMock.Setup(d => d.UtcNow).Returns(_now);
                _instrumentServiceMock.Setup(s => s.GetInstruments())
                    .Returns([
                        new TestInstrument(
                            "E500-123",
                            Pharos.Server.Domain.Enums.InstrumentType.Small,
                            "E500",
                            instrumentSoftwareVersion: "1.2.3",
                            state: Pharos.Server.Domain.Enums.InstrumentState.Standby)
                    ]);

                _sut = new InstrumentStatusSnapshotProvider(
                    _instrumentServiceMock.Object,
                    dateTimeMock.Object,
                    _maintenanceActivityCache);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Fact]
            public void CreatesSnapshotWithReportedAtFromClock()
            {
                // Act
                var snapshot = _sut.CreateSnapshot();

                // Assert
                Assert.Equal(_now, snapshot.ReportedAt);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsInstrumentInfoAndCurrentState()
            {
                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                Assert.Equal(InstrumentType.E500, instrument.Type);
                Assert.Equal("E500-123", instrument.SerialNumber);
                Assert.Equal("Standby", instrument.Software.State);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsInstrumentSoftwareVersion()
            {
                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                Assert.Equal("1.2.3", instrument.Software.Version);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsAllSensorReadings()
            {
                // Arrange
                var allSensorTypes = Enum.GetValues<EnumContinuousSensorType>();
                var sensorReadings = allSensorTypes
                    .Select((sensorType, index) => new ContinuousSensor { Type = sensorType, Value = index })
                    .ToList();
                _instrumentServiceMock.Setup(s => s.GetInstruments())
                    .Returns([
                        new TestInstrument(
                            " E500-123 ",
                            Pharos.Server.Domain.Enums.InstrumentType.Small,
                            "E500")
                        {
                            EnvironmentSensors = sensorReadings
                        }
                    ]);

                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                instrument.SensorReadings.Select(reading => (reading.SensorType, reading.Value)).Should()
                    .Equal(sensorReadings.Select(reading => (reading.Type.ToString(), (double?)reading.Value)));
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsCachedMaintenanceInformation()
            {
                // Arrange
                var lastStarted = new DateTimeOffset(2026, 8, 10, 7, 0, 0, TimeSpan.Zero);
                var lastEnded = new DateTimeOffset(2026, 8, 10, 7, 30, 0, TimeSpan.Zero);
                _maintenanceActivityCache.Set(
                    "E500-123",
                    new MaintenanceActivity
                    {
                        MaintenanceActivities =
                        [
                            new MaintenanceActivityInformation
                            {
                                Type = "DailyRinse",
                                Status = "MaintenanceStatusOk",
                                LastStarted = lastStarted,
                                LastEnded = lastEnded
                            }
                        ],
                        Rinsed = true,
                        MaintenanceNeeded = false
                    });

                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                var maintenanceInformation = Assert.IsType<MaintenanceActivity>(
                    instrument.MaintenanceActivity);
                var activity = Assert.Single(maintenanceInformation.MaintenanceActivities);
                Assert.Equal("DailyRinse", activity.Type);
                Assert.Equal("MaintenanceStatusOk", activity.Status);
                Assert.Equal(lastStarted, activity.LastStarted);
                Assert.Equal(lastEnded, activity.LastEnded);
                Assert.True(maintenanceInformation.Rinsed);
                Assert.False(maintenanceInformation.MaintenanceNeeded);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsUnknownInstrumentType()
            {
                // Arrange
                _instrumentServiceMock.Setup(s => s.GetInstruments())
                    .Returns([
                        new TestInstrument("UNKNOWN-1", Pharos.Server.Domain.Enums.InstrumentType.Unknown, "Unknown")
                    ]);

                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                Assert.Equal(InstrumentType.Unknown, instrument.Type);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsMediumInstrumentType()
            {
                // Arrange
                _instrumentServiceMock.Setup(s => s.GetInstruments())
                    .Returns([
                        new TestInstrument("E2000-123", Pharos.Server.Domain.Enums.InstrumentType.Medium, "E2000")
                    ]);

                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                Assert.Equal(InstrumentType.E2000, instrument.Type);
            }

            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6019")]
            [Fact]
            public void MapsInstrumentInstallationDate()
            {
                // Arrange
                var installationDate = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
                _instrumentServiceMock.Setup(s => s.GetInstruments())
                    .Returns([
                        new TestInstrument("E2000-123", Pharos.Server.Domain.Enums.InstrumentType.Medium, "E2000", null, null, installationDate)
                    ]);

                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                Assert.Equal(installationDate, instrument.InstallationDate);
            }

            [Trait("ProductRequirement", "PR_552")]
            [Trait("ProductRequirement", "PR_2192")]
            [Trait("ModuleRequirement", "MR_6052")]
            [Fact]
            public void MapsInstrumentModuleInformation()
            {
                // Arrange
                var moduleInstallationDate = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
                var mockInstrument = new TestInstrument("E2000-123", Pharos.Server.Domain.Enums.InstrumentType.Medium, "E2000")
                {
                    Modules = new List<HardwareModule>
                    {
                        new()
                        {
                            ModuleType = ModuleType.BarcodeReader,
                            ArticleNumber = "abc_123",
                            Model = "model",
                            Version = "2.0",
                            SerialNumber = "b123456",
                            InstallationDate = moduleInstallationDate
                        }
                    }
                };

                _instrumentServiceMock.Setup(s => s.GetInstruments())
                    .Returns([mockInstrument]);

                // Act
                var instrument = Assert.Single(_sut.CreateSnapshot().Instruments);

                // Assert
                instrument.Modules.Should().ContainSingle(module => module.ModuleType == "BarcodeReader" &&
                                                                    module.ArticleNumber == "abc_123" &&
                                                                    module.Model == "model" &&
                                                                    module.Version == "2.0" &&
                                                                    module.SerialNumber == "b123456" &&
                                                                    module.InstallationDate == moduleInstallationDate);
            }
        }
    }
}
