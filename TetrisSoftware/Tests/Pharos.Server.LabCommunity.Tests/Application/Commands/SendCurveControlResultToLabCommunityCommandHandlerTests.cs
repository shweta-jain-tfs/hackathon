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

public static class SendCurveControlResultToLabCommunityCommandHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<ILabCommunityExportQueue> _queueMock = new();
        private readonly SendCurveControlResultToLabCommunityCommand _command;
        private readonly SendCurveControlResultToLabCommunityCommandHandler _sut;
        private string? _payloadJson;

        public TheHandleMethod()
        {
            const string laboratoryCode = "123456";
            var measurementTime = new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
            _command = new SendCurveControlResultToLabCommunityCommand(
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
                [new CurveControlResultReplicate(1, measurementTime, 12.8, 100.2, CurveControlResultJudgment.Ok, "CON1", "SOL1", "CCL1")]);

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

            _sut = new SendCurveControlResultToLabCommunityCommandHandler(_queueMock.Object, dbContextMock.Object, settingsSnapshotMock.Object);
        }

        [Trait("ProductRequirement", "PR_2183")]
        [Trait("ModuleRequirement", "MR_6056")]
        [Fact]
        public async Task EnqueuesFullResult()
        {
            await _sut.Handle(_command, TestContext.Current.CancellationToken);

            _queueMock.Verify(queue => queue.EnqueueAsync(
                ExportItemType.CurveControlResult,
                $"CurveControlResult:{_command.GroupId}:{_command.CurveControlName}",
                It.IsAny<string>(),
                1,
                It.IsAny<CancellationToken>()), Times.Once);

            JsonSerializer.Deserialize<SendCurveControlResultToLabCommunityCommand>(_payloadJson!).Should().BeEquivalentTo(_command);
        }
    }
}