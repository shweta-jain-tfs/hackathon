using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Pharos.Server.Application.LabCommunity;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Commands;
using Pharos.Server.LabCommunity.Application.Interfaces;
using Pharos.Server.LabCommunity.Domain;
using Xunit;

namespace Pharos.Server.LabCommunity.Tests.Application.Commands;

public static class SendQualityControlResultToLabCommunityCommandHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<ILabCommunityExportQueue> _queueMock = new();
        private readonly SendQualityControlResultToLabCommunityCommand _command;
        private readonly SendQualityControlResultToLabCommunityCommandHandler _sut;
        private string? _payloadJson;

        public TheHandleMethod()
        {
            const string laboratoryCode = "123456";
            var measurementTime = new DateTimeOffset(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);
            _command = new SendQualityControlResultToLabCommunityCommand(
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
                [new QualityControlResultReplicate(1, measurementTime, 12.8, 100.2, QualityControlResultJudgment.HighCv, "CON1", "SOL1", "QCL1")]);

            _queueMock.Setup(queue => queue.EnqueueAsync(
                    It.IsAny<ExportItemType>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ExportItemType, string, string, int, CancellationToken>((_, _, payload, _, _) => _payloadJson = payload);
            var dbContextMock = new Mock<ILabCommunityDbContext>();
            dbContextMock.Setup(db => db.GetConfigurationAsync(laboratoryCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Configuration { ReportCalibrationAndQualityResultsEnabled = true });
            var settingsSnapshotMock = new Mock<IOptionsSnapshot<LabCommunitySettings>>();
            settingsSnapshotMock.Setup(snapshot => snapshot.Value).Returns(new LabCommunitySettings
            {
                Enable = true,
                LaboratoryCode = laboratoryCode
            });

            _sut = new SendQualityControlResultToLabCommunityCommandHandler(_queueMock.Object, dbContextMock.Object, settingsSnapshotMock.Object);
        }

        [Trait("ProductRequirement", "PR_2184")]
        [Trait("ModuleRequirement", "MR_6059")]
        [Fact]
        public async Task EnqueuesFullResult()
        {
            await _sut.Handle(_command, TestContext.Current.CancellationToken);

            _queueMock.Verify(queue => queue.EnqueueAsync(
                ExportItemType.QualityControlResult,
                $"QualityControlResult:{_command.GroupId}:{_command.QualityControlName}",
                It.IsAny<string>(),
                1,
                It.IsAny<CancellationToken>()), Times.Once);

            JsonSerializer.Deserialize<SendQualityControlResultToLabCommunityCommand>(_payloadJson!).Should().BeEquivalentTo(_command);
        }
    }
}
