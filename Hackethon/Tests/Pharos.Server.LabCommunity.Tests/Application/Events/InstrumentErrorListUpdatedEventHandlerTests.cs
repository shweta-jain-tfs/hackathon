using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.LabCommunity.Application.Events;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Tetris.Protocol.Pharos.Errors;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Events
{
    public static class InstrumentErrorListUpdatedEventHandlerTests
    {
        public class TheHandleMethod
        {
            private readonly Mock<ILabCommunityExportQueue> _exportQueueMock;
            private readonly Mock<ILabCommunityDbContext> _dbContextMock;
            private readonly Mock<IOptionsSnapshot<LabCommunitySettings>> _settingsSnapshotMock;
            private readonly Mock<IInstrumentManager> _instrumentServiceMock;
            private readonly InstrumentErrorListUpdatedEventHandler _sut;

            public TheHandleMethod()
            {
                _exportQueueMock = new Mock<ILabCommunityExportQueue>();
                _dbContextMock = new Mock<ILabCommunityDbContext>();
                _settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
                _instrumentServiceMock = new Mock<IInstrumentManager>();

                _sut = new InstrumentErrorListUpdatedEventHandler(
                    _exportQueueMock.Object,
                    _dbContextMock.Object,
                    _settingsSnapshotMock.Object,
                    _instrumentServiceMock.Object);
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task SkipsExportItem_WhenInstrumentErrorReportingDisabledInPulledConfiguration()
            {
                // Arrange
                _settingsSnapshotMock.Setup(s => s.Value).Returns(new LabCommunitySettings
                {
                    Enable = true,
                    LaboratoryCode = "123456"
                });
                _dbContextMock.Setup(db => db.GetConfigurationAsync("123456", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Configuration { ReportInstrumentErrorsEnabled = false });

                var notification = new InstrumentErrorListUpdatedEvent(
                    "SN123",
                    new CurrentErrorList
                    {
                        Errors =
                        {
                            new Error
                            {
                                ErrorId = "4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                                Code = 123
                            }
                        }
                    });

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _exportQueueMock.Verify(q => q.EnqueueAsync(
                    It.IsAny<ExportItemType>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()), Times.Never);
            }

            [Trait("ProductRequirement", "PR_2188")]
            [Trait("ModuleRequirement", "MR_5998")]
            [Fact]
            public async Task EnqueuesExportItem_WhenInstrumentErrorReportingEnabled()
            {
                // Arrange
                var errorTime = new DateTimeOffset(2026, 5, 15, 7, 0, 0, TimeSpan.Zero);
                string? payloadJson = null;

                _settingsSnapshotMock.Setup(s => s.Value).Returns(new LabCommunitySettings
                {
                    Enable = true,
                    LaboratoryCode = "123456"
                });
                _dbContextMock.Setup(db => db.GetConfigurationAsync("123456", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Configuration { ReportInstrumentErrorsEnabled = true });
                _instrumentServiceMock.Setup(s => s.FindInstrument("SN123"))
                    .Returns(new TestInstrument("SN123", InstrumentType.Small, "Phadia E500"));
                _exportQueueMock.Setup(q => q.EnqueueAsync(
                        It.IsAny<ExportItemType>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<int>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<ExportItemType, string, string, int, CancellationToken>((_, _, payload, _, _) => payloadJson = payload)
                    .Returns(Task.CompletedTask);

                var notification = new InstrumentErrorListUpdatedEvent(
                    "SN123",
                    new CurrentErrorList
                    {
                        Errors =
                        {
                            new Error
                            {
                                ErrorId = "4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                                Code = 123,
                                Classification = EnumErrorClassification.Warning,
                                Timestamp = Timestamp.FromDateTimeOffset(errorTime)
                            }
                        }
                    });

                // Act
                await _sut.Handle(notification, TestContext.Current.CancellationToken);

                // Assert
                _exportQueueMock.Verify(q => q.EnqueueAsync(
                    ExportItemType.InstrumentError,
                    "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                    It.IsAny<string>(),
                    1,
                    It.IsAny<CancellationToken>()), Times.Once);

                using var payload = JsonDocument.Parse(payloadJson!);
                var root = payload.RootElement;
                Assert.Equal((int)InstrumentType.Small, root.GetProperty("InstrumentType").GetInt32());
                Assert.Equal("SN123", root.GetProperty("SerialNumber").GetString());
                Assert.Equal("123", root.GetProperty("ErrorCode").GetString());
                Assert.Equal("TODO: Instrument error text.", root.GetProperty("ErrorText").GetString());
                Assert.Equal(errorTime, root.GetProperty("ErrorTime").GetDateTimeOffset());
                Assert.Equal((int)ErrorClassification.Warning, root.GetProperty("Classification").GetInt32());
            }
        }
    }
}
