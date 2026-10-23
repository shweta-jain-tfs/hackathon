using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Application.Jobs;
using Quartz;

namespace Pharos.Server.Lis.Tests.Application.Jobs;

[Trait("ProductRequirement", "PR_565")]
[Trait("ModuleRequirement", "MR_5866")]
public class ResetStatisticsJobTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<IJobExecutionContext> _contextMock;
    private readonly Mock<IOptionsMonitor<LisSettings>> _lisSettingsMock;
    private readonly ResetLisStatisticsJob _sut;

    public ResetStatisticsJobTests()
    {
        _senderMock = new Mock<ISender>();
        _contextMock = new Mock<IJobExecutionContext>();
        _lisSettingsMock = new Mock<IOptionsMonitor<LisSettings>>();

        _contextMock.Setup(x => x.CancellationToken).Returns(TestContext.Current.CancellationToken);

        _sut = new ResetLisStatisticsJob(
            _senderMock.Object,
            _lisSettingsMock.Object,
            NullLogger<ResetLisStatisticsJob>.Instance);
    }

    [Fact]
    public async Task Execute_Should_SendResetStatisticsCommand_For_Each_Channel_With_Setting_Enabled()
    {
        // Arrange
        var settings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel { Other = new LisOtherSettings { ResetStatisticsAtMidnight = true } },
                new LisChannel { Other = new LisOtherSettings { ResetStatisticsAtMidnight = false } },
                new LisChannel { Other = new LisOtherSettings { ResetStatisticsAtMidnight = true } },
            ]
        };
        _lisSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        // Act
        await _sut.Execute(_contextMock.Object);

        // Assert
        _senderMock.Verify(s => s.Send(It.Is<ResetLisStatisticsCommand>(cmd => cmd.ChannelId == 0), TestContext.Current.CancellationToken), Times.Once);
        _senderMock.Verify(s => s.Send(It.Is<ResetLisStatisticsCommand>(cmd => cmd.ChannelId == 1), TestContext.Current.CancellationToken), Times.Never);
        _senderMock.Verify(s => s.Send(It.Is<ResetLisStatisticsCommand>(cmd => cmd.ChannelId == 2), TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task Execute_Should_NotSendAnyCommand_When_No_Channels_Have_Setting_Enabled()
    {
        // Arrange
        var settings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel { Other = new LisOtherSettings { ResetStatisticsAtMidnight = false } },
                new LisChannel { Other = new LisOtherSettings { ResetStatisticsAtMidnight = false } },
            ]
        };
        _lisSettingsMock.Setup(x => x.CurrentValue).Returns(settings);

        // Act
        await _sut.Execute(_contextMock.Object);

        // Assert
        _senderMock.Verify(s => s.Send(It.IsAny<ResetLisStatisticsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_Should_NotThrow_When_SenderThrowsException()
    {
        // Arrange
        var settings = new LisSettings
        {
            LisChannels = [new LisChannel { Other = new LisOtherSettings { ResetStatisticsAtMidnight = true } }]
        };
        _lisSettingsMock.Setup(x => x.CurrentValue).Returns(settings);
        _senderMock
            .Setup(s => s.Send(It.IsAny<ResetLisStatisticsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unexpected failure"));

        // Act
        var act = async () => await _sut.Execute(_contextMock.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
