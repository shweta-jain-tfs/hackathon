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

public static class SendBlankResultToLabCommunityCommandHandlerTests
{
    public class TheHandleMethod
    {
        private readonly Mock<ILabCommunityExportQueue> _queueMock = new();
        private readonly SendBlankResultToLabCommunityCommand _command;
        private readonly SendBlankResultToLabCommunityCommandHandler _sut;
        private string? _payloadJson;

        public TheHandleMethod()
        {
            const string laboratoryCode = "123456";
            var measurementTime = new DateTimeOffset(2026, 10, 8, 9, 0, 0, TimeSpan.Zero);
            _command = new SendBlankResultToLabCommunityCommand(
                1,
                "SN123",
                measurementTime,
                BlankResultType.ReagentBlankEliA,
                false,
                "DEV-BARCODE",
                "STOP-BARCODE",
                [new BlankResultReplicate(1, measurementTime, 100.2)]);

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

            _sut = new SendBlankResultToLabCommunityCommandHandler(_queueMock.Object, dbContextMock.Object, settingsSnapshotMock.Object);
        }

        [Trait("ProductRequirement", "PR_2185")]
        [Trait("ModuleRequirement", "MR_6230")]
        [Fact]
        public async Task EnqueuesFullResult()
        {
            await _sut.Handle(_command, TestContext.Current.CancellationToken);

            _queueMock.Verify(queue => queue.EnqueueAsync(
                ExportItemType.BlankResult,
                $"BlankResult:{_command.BlankId}",
                It.IsAny<string>(),
                1,
                It.IsAny<CancellationToken>()), Times.Once);

            JsonSerializer.Deserialize<SendBlankResultToLabCommunityCommand>(_payloadJson!).Should().BeEquivalentTo(_command);
        }
    }
}