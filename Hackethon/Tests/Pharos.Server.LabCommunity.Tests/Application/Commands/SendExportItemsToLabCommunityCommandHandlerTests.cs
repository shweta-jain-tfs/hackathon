using System.Net;
using System.Text.Json;
using FluentAssertions;
using LabCommunity.API.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Infrastructure;
using Pharos.Server.Integration.Tests.Common;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Application.Models;
using Pharos.Server.LabCommunity.Domain;
using Xunit;
using ArticleType = Pharos.Server.Domain.Enums.ArticleType;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands;

public static class SendExportItemsToLabCommunityCommandHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<ILabCommunityDbContext> _dbContextMock = new();
        private readonly Mock<ILabCommunityExportQueue> _exportQueueMock = new();
        private readonly Mock<ILabCommunityApiClient> _clientMock = new();
        private readonly Mock<IInstrumentManager> _instrumentServiceMock = new();
        private readonly Mock<IReagentBarcodeService> _reagentBarcodeServiceMock = new();
        private readonly Mock<IDateTime> _dateTimeMock = new();
        private ReportInstrumentErrorRequest? _instrumentErrorRequest;
        private ReportCurveControlResultRequest? _curveControlResultRequest;
        private ReportQualityControlResultRequest? _qualityControlResultRequest;
        private ReportBlankResultRequest? _blankResultRequest;
        private readonly LabCommunitySettings _settings = new()
        {
            LaboratoryCode = "123456"
        };
        private readonly SendExportItemsToLabCommunityCommandHandler _sut;

        public TheHandleMethod()
        {
            var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
            settingsSnapshotMock.Setup(snapshot => snapshot.Value).Returns(_settings);

            _dbContextMock
                .Setup(dbContext => dbContext.GetActiveEnrollmentAsync(_settings.LaboratoryCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Enrollment { LaboratoryCode = _settings.LaboratoryCode });
            _clientMock
                .Setup(client => client.ReportInstrumentErrorAsync(
                    It.IsAny<ReportInstrumentErrorRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ReportInstrumentErrorRequest, CancellationToken>((request, _) => _instrumentErrorRequest = request);
            _clientMock
                .Setup(client => client.ReportCurveControlResultAsync(
                    It.IsAny<ReportCurveControlResultRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ReportCurveControlResultRequest, CancellationToken>((request, _) => _curveControlResultRequest = request);
            _clientMock
                .Setup(client => client.ReportQualityControlResultAsync(
                    It.IsAny<ReportQualityControlResultRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ReportQualityControlResultRequest, CancellationToken>((request, _) => _qualityControlResultRequest = request);
            _clientMock
                .Setup(client => client.ReportBlankResultAsync(
                    It.IsAny<ReportBlankResultRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ReportBlankResultRequest, CancellationToken>((request, _) => _blankResultRequest = request);

            _sut = new SendExportItemsToLabCommunityCommandHandler(
                _dbContextMock.Object,
                _exportQueueMock.Object,
                _clientMock.Object,
                _instrumentServiceMock.Object,
                _reagentBarcodeServiceMock.Object,
                settingsSnapshotMock.Object,
                _dateTimeMock.Object,
                Mock.Of<ILogger<SendExportItemsToLabCommunityCommandHandler>>());
        }

        [Trait("ProductRequirement", "PR_2188")]
        [Trait("ModuleRequirement", "MR_5998")]
        [Fact]
        public async Task SendsInstrumentErrorAndDequeuesItem()
        {
            // Arrange
            var errorTime = new DateTimeOffset(2026, 5, 15, 7, 0, 0, TimeSpan.Zero);
            var item = CreateInstrumentErrorItem(errorTime, Pharos.Server.Domain.Enums.ErrorClassification.Fatal);
            SetupExportItem(item, errorTime.AddHours(3));

            // Act
            await _sut.Handle(
                new SendExportItemsToLabCommunityCommand(item.ExportItemType),
                TestContext.Current.CancellationToken);

            // Assert
            _exportQueueMock.Verify(
                queue => queue.DequeueBatchAsync(
                    It.Is<IReadOnlyCollection<ExportItem>>(items => items.Count == 1 && items.Contains(item)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _instrumentErrorRequest.Should().BeEquivalentTo(new ReportInstrumentErrorRequest
            {
                InstrumentType = InstrumentType.E500,
                SerialNumber = "SN123",
                ErrorCode = "123",
                ErrorText = "Instrument error text.",
                ErrorTime = errorTime,
                Classification = InstrumentErrorClassification.Fatal
            });
        }

        [Trait("ProductRequirement", "PR_2183")]
        [Trait("ModuleRequirement", "MR_6056")]
        [Fact]
        public async Task SendsCurveControlResultAndDequeuesItem()
        {
            // Arrange
            var measurementTime = new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
            var item = CreateCurveControlItem(new SendCurveControlResultToLabCommunityCommand(
                Guid.NewGuid(),
                "SN123",
                measurementTime,
                "CC1",
                "Method1",
                "Test1",
                CurveControlResultJudgment.FlaggedHigh,
                12.5,
                "CON1",
                "SOL1",
                "CCL1",
                [
                    new CurveControlResultReplicate(
                        1,
                        measurementTime,
                        12.8,
                        100.2,
                        CurveControlResultJudgment.Ok,
                        "CON1",
                        "SOL1",
                        "CCL1"),
                    new CurveControlResultReplicate(
                        2,
                        measurementTime.AddMinutes(1),
                        null,
                        null,
                        CurveControlResultJudgment.Low,
                        "CON2",
                        "SOL2",
                        "CCL2")
                ]));
            SetupExportItem(
                item,
                measurementTime.AddMinutes(2),
                new TestInstrument("SN123", Pharos.Server.Domain.Enums.InstrumentType.Small, "E500"));

            // Act
            await _sut.Handle(
                new SendExportItemsToLabCommunityCommand(item.ExportItemType),
                TestContext.Current.CancellationToken);

            // Assert
            _exportQueueMock.Verify(
                queue => queue.DequeueBatchAsync(
                    It.Is<IReadOnlyCollection<ExportItem>>(items => items.Count == 1 && items.Contains(item)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _curveControlResultRequest.Should().BeEquivalentTo(new ReportCurveControlResultRequest
            {
                InstrumentType = InstrumentType.E500,
                SerialNumber = "SN123",
                MeasurementTime = measurementTime,
                CurveControlName = "CC1",
                MethodName = "Method1",
                TestName = "Test1",
                Judgment = CurveControlJudgment.FlaggedHigh,
                ConjugateLotNumber = "CON1",
                SolidPhaseLotNumber = "SOL1",
                CurveControlLotNumber = "CCL1",
                Replicates =
                [
                    new CurveControlReplicateRequest
                    {
                        ReplicateNumber = 1,
                        MeasurementTime = measurementTime,
                        Concentration = 12.8,
                        DefinedConcentration = 12.5,
                        Response = 100.2,
                        Judgment = CurveControlJudgment.Ok,
                        ConjugateLotNumber = "CON1",
                        SolidPhaseLotNumber = "SOL1",
                        CurveControlLotNumber = "CCL1"
                    },
                    new CurveControlReplicateRequest
                    {
                        ReplicateNumber = 2,
                        MeasurementTime = measurementTime.AddMinutes(1),
                        Concentration = null,
                        DefinedConcentration = 12.5,
                        Response = null,
                        Judgment = CurveControlJudgment.Low,
                        ConjugateLotNumber = "CON2",
                        SolidPhaseLotNumber = "SOL2",
                        CurveControlLotNumber = "CCL2"
                    }
                ]
            });
        }

        [Trait("ProductRequirement", "PR_2184")]
        [Trait("ModuleRequirement", "MR_6059")]
        [Fact]
        public async Task SendsQualityControlResultAndDequeuesItem()
        {
            // Arrange
            var measurementTime = new DateTimeOffset(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);
            var item = CreateQualityControlItem(new SendQualityControlResultToLabCommunityCommand(
                Guid.NewGuid(),
                "SN123",
                measurementTime,
                "QC1",
                "Method1",
                "Test1",
                QualityControlResultJudgment.HighCv,
                "CON1",
                "SOL1",
                "QCL1",
                11.5,
                13.5,
                15,
                [
                    new QualityControlResultReplicate(
                        1,
                        measurementTime,
                        12.8,
                        100.2,
                        QualityControlResultJudgment.FlaggedHigh,
                        "CON1",
                        "SOL1",
                        "QCL1"),
                    new QualityControlResultReplicate(
                        2,
                        measurementTime.AddMinutes(1),
                        null,
                        null,
                        QualityControlResultJudgment.Low,
                        "CON2",
                        "SOL2",
                        "QCL2")
                ]));
            SetupExportItem(
                item,
                measurementTime.AddMinutes(2),
                new TestInstrument("SN123", Pharos.Server.Domain.Enums.InstrumentType.Medium, "E2000"));

            // Act
            await _sut.Handle(
                new SendExportItemsToLabCommunityCommand(item.ExportItemType),
                TestContext.Current.CancellationToken);

            // Assert
            _exportQueueMock.Verify(
                queue => queue.DequeueBatchAsync(
                    It.Is<IReadOnlyCollection<ExportItem>>(items => items.Count == 1 && items.Contains(item)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _qualityControlResultRequest.Should().BeEquivalentTo(new ReportQualityControlResultRequest
            {
                InstrumentType = InstrumentType.E2000,
                SerialNumber = "SN123",
                MeasurementTime = measurementTime,
                QualityControlName = "QC1",
                MethodName = "Method1",
                TestName = "Test1",
                Judgment = QualityControlJudgment.HighCv,
                ConjugateLotNumber = "CON1",
                SolidPhaseLotNumber = "SOL1",
                QualityControlLotNumber = "QCL1",
                RangeMin = 11.5,
                RangeMax = 13.5,
                PercentCvLimit = 15,
                Replicates =
                [
                    new QualityControlReplicateRequest
                    {
                        ReplicateNumber = 1,
                        MeasurementTime = measurementTime,
                        Concentration = 12.8,
                        Response = 100.2,
                        Judgment = QualityControlJudgment.FlaggedHigh,
                        ConjugateLotNumber = "CON1",
                        SolidPhaseLotNumber = "SOL1",
                        QualityControlLotNumber = "QCL1"
                    },
                    new QualityControlReplicateRequest
                    {
                        ReplicateNumber = 2,
                        MeasurementTime = measurementTime.AddMinutes(1),
                        Concentration = null,
                        Response = null,
                        Judgment = QualityControlJudgment.Low,
                        ConjugateLotNumber = "CON2",
                        SolidPhaseLotNumber = "SOL2",
                        QualityControlLotNumber = "QCL2"
                    }
                ]
            });
        }

        [Trait("ProductRequirement", "PR_2185")]
        [Trait("ModuleRequirement", "MR_6230")]
        [Fact]
        public async Task SendsBlankResultAndDequeuesItem()
        {
            // Arrange
            var measurementTime = new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero);
            var item = CreateBlankItem(new SendBlankResultToLabCommunityCommand(
                1,
                "SN123",
                measurementTime,
                BlankResultType.ReagentBlankEliA,
                false,
                "DEV-BARCODE",
                "STOP-BARCODE",
                [new BlankResultReplicate(1, measurementTime, 100.2)]));
            _reagentBarcodeServiceMock
                .Setup(service => service.GetReagentLotInformation(
                    ArticleType.DevelopmentSolutionCartridge,
                    "DEV-BARCODE",
                    null))
                .Returns(new LotInformation { LotNumber = "DEV1" });
            _reagentBarcodeServiceMock
                .Setup(service => service.GetReagentLotInformation(
                    ArticleType.StopSolutionBottle,
                    null,
                    "STOP-BARCODE"))
                .Returns(new LotInformation { LotNumber = "STOP1" });
            SetupExportItem(
                item,
                measurementTime.AddMinutes(2),
                new TestInstrument("SN123", Pharos.Server.Domain.Enums.InstrumentType.Medium, "E2000"));

            // Act
            await _sut.Handle(
                new SendExportItemsToLabCommunityCommand(item.ExportItemType),
                TestContext.Current.CancellationToken);

            // Assert
            _exportQueueMock.Verify(
                queue => queue.DequeueBatchAsync(
                    It.Is<IReadOnlyCollection<ExportItem>>(items => items.Count == 1 && items.Contains(item)),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _blankResultRequest.Should().BeEquivalentTo(new ReportBlankResultRequest
            {
                InstrumentType = InstrumentType.E2000,
                SerialNumber = "SN123",
                MeasurementTime = measurementTime,
                BlankType = BlankType.ReagentBlankEliA,
                AssayTechnology = AssayTechnology.EliA,
                Judgment = BlankJudgment.OutOfLimits,
                DevelopmentSolutionLotNumber = "DEV1",
                StopSolutionLotNumber = "STOP1",
                Replicates =
                [
                    new BlankReplicateRequest
                    {
                        ReplicateNumber = 1,
                        MeasurementTime = measurementTime,
                        Voltage = 100.2,
                        Judgment = BlankJudgment.OutOfLimits,
                        DevelopmentSolutionLotNumber = "DEV1",
                        StopSolutionLotNumber = "STOP1"
                    }
                ]
            });
        }

        [Fact]
        public async Task DequeuesExportItemRejectedByLabCommunity()
        {
            var now = new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero);
            var item = CreateInstrumentErrorItem(now, Pharos.Server.Domain.Enums.ErrorClassification.Error);
            SetupExportItem(item, now);
            _clientMock
                .Setup(client => client.ReportInstrumentErrorAsync(
                    It.IsAny<ReportInstrumentErrorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException(
                    "Unprocessable Entity",
                    (int)HttpStatusCode.UnprocessableEntity,
                    null,
                    new Dictionary<string, IEnumerable<string>>(),
                    null!));

            await _sut.Handle(
                new SendExportItemsToLabCommunityCommand(item.ExportItemType),
                TestContext.Current.CancellationToken);

            _exportQueueMock.Verify(
                queue => queue.DequeueBatchAsync(
                    It.Is<IReadOnlyCollection<ExportItem>>(items => items.Count == 1 && items.Contains(item)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Trait("ProductRequirement", "PR_2188")]
        [Trait("ModuleRequirement", "MR_5998")]
        [Fact]
        public async Task KeepsFailedItemForRetry()
        {
            var now = new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero);
            var item = CreateInstrumentErrorItem(now, Pharos.Server.Domain.Enums.ErrorClassification.Error);
            var error = new string('x', Pharos.Server.Domain.Constants.ExportItemLastErrorLength + 10);
            SetupExportItem(item, now);
            _clientMock
                .Setup(client => client.ReportInstrumentErrorAsync(
                    It.IsAny<ReportInstrumentErrorRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(error));

            await _sut.Handle(
                new SendExportItemsToLabCommunityCommand(ExportItemType.InstrumentError),
                TestContext.Current.CancellationToken);

            Assert.Equal(ExportItemStatus.Failed, item.Status);
            Assert.Equal(1, item.AttemptCount);
            Assert.Equal(now, item.LastAttemptAtUtc);
            Assert.Equal(now.AddMinutes(5), item.NextAttemptAtUtc);
            Assert.Equal(error[..Pharos.Server.Domain.Constants.ExportItemLastErrorLength], item.LastError);
            _exportQueueMock.Verify(
                queue => queue.DequeueBatchAsync(
                    It.Is<IReadOnlyCollection<ExportItem>>(items => items.Count == 0),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void SetupExportItem(
            ExportItem item,
            DateTimeOffset utcNow,
            TestInstrument? instrument = null)
        {
            _exportQueueMock
                .Setup(queue => queue.PeekBatchAsync(item.ExportItemType, It.IsAny<CancellationToken>()))
                .ReturnsAsync([item]);
            _dateTimeMock.Setup(clock => clock.UtcNow).Returns(utcNow);

            if (instrument is not null)
            {
                _instrumentServiceMock
                    .Setup(service => service.GetInstrument("SN123"))
                    .Returns(instrument);
            }
        }

        private static ExportItem CreateInstrumentErrorItem(
            DateTimeOffset errorTime,
            Pharos.Server.Domain.Enums.ErrorClassification classification)
        {
            return new ExportItem
            {
                ExportItemType = ExportItemType.InstrumentError,
                DeduplicationKey = "InstrumentError:SN123:4b0f3d55-1790-4c98-9a5c-21b8edff101a",
                PayloadVersion = 1,
                Status = ExportItemStatus.Pending,
                CreatedAtUtc = errorTime,
                PayloadJson = JsonSerializer.Serialize(new InstrumentErrorExportPayload
                {
                    InstrumentType = Pharos.Server.Domain.Enums.InstrumentType.Small,
                    SerialNumber = "SN123",
                    ErrorCode = "123",
                    ErrorText = "Instrument error text.",
                    ErrorTime = errorTime,
                    Classification = classification
                })
            };
        }

        private static ExportItem CreateCurveControlItem(SendCurveControlResultToLabCommunityCommand command)
        {
            return new ExportItem
            {
                ExportItemType = ExportItemType.CurveControlResult,
                DeduplicationKey = $"CurveControlResult:{command.GroupId}:{command.CurveControlName}",
                PayloadVersion = 1,
                Status = ExportItemStatus.Pending,
                CreatedAtUtc = command.MeasurementTime,
                PayloadJson = JsonSerializer.Serialize(command)
            };
        }

        private static ExportItem CreateQualityControlItem(SendQualityControlResultToLabCommunityCommand command)
        {
            return new ExportItem
            {
                ExportItemType = ExportItemType.QualityControlResult,
                DeduplicationKey = $"QualityControlResult:{command.GroupId}:{command.QualityControlName}",
                PayloadVersion = 1,
                Status = ExportItemStatus.Pending,
                CreatedAtUtc = command.MeasurementTime,
                PayloadJson = JsonSerializer.Serialize(command)
            };
        }

        private static ExportItem CreateBlankItem(SendBlankResultToLabCommunityCommand command)
        {
            return new ExportItem
            {
                ExportItemType = ExportItemType.BlankResult,
                DeduplicationKey = $"BlankResult:{command.BlankId}",
                PayloadVersion = 1,
                Status = ExportItemStatus.Pending,
                CreatedAtUtc = command.MeasurementTime,
                PayloadJson = JsonSerializer.Serialize(command)
            };
        }
    }
}