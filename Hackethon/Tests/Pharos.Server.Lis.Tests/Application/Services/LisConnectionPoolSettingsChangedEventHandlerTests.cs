using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands.LisStatisticsCommands;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Services;

namespace Pharos.Server.Lis.Tests.Application.Services;

[Trait("ProductRequirement", "PR_565")]
public class LisConnectionPoolSettingsChangedEventHandlerTests
{
    [Fact]
    [Trait("ModuleRequirement", "MR_5380")]
    public async Task Handle_Should_NotDeleteStatistics_WhenChannelIsOnlyRenamed()
    {
        var optionsMonitor = new Mock<IOptionsMonitor<LisSettings>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new LisSettings());

        var connectionFactory = new Mock<ILisConnectionFactory>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var sender = new Mock<ISender>();

        var connection = new Mock<ILisConnection>();
        connection.Setup(x => x.OpenAsync()).ReturnsAsync(true);
        connection.Setup(x => x.Role).Returns(LisConnectionRole.Both);
        connection.Setup(x => x.State).Returns(LisConnectionState.Open);
        connectionFactory
            .Setup(x => x.CreateConnection(It.IsAny<int>(), It.IsAny<LisChannel>(), It.IsAny<LisConnectionRole>()))
            .Returns(connection.Object);

        using var connectionPool = new LisConnectionPool(
            optionsMonitor.Object,
            connectionFactory.Object,
            scopeFactory.Object,
            NullLogger<LisConnectionPool>.Instance);

        var sut = new LisConnectionPool.LisSettingsChangedEventHandler(
            connectionPool,
            sender.Object,
            NullLogger<LisConnectionPool.LisSettingsChangedEventHandler>.Instance);

        var oldSettings = new LisSettings
        {
            LisChannels = [new LisChannel { Name = "Old Name", IsEnabled = true, ProtocolType = LisProtocolType.Astm }]
        };
        var newSettings = new LisSettings
        {
            LisChannels = [new LisChannel { Name = "New Name", IsEnabled = true, ProtocolType = LisProtocolType.Astm }]
        };

        await sut.Handle(new SettingsChangedEvent<LisSettings>(oldSettings, newSettings), TestContext.Current.CancellationToken);

        sender.Verify(
            x => x.Send(It.IsAny<DeleteLisStatisticsCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5377")]
    public async Task Handle_Should_DeleteStatistics_WhenChannelIsRemoved()
    {
        var optionsMonitor = new Mock<IOptionsMonitor<LisSettings>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new LisSettings());

        var connectionFactory = new Mock<ILisConnectionFactory>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var sender = new Mock<ISender>();
        var connection = new Mock<ILisConnection>();
        connection.Setup(x => x.OpenAsync()).ReturnsAsync(true);
        connection.Setup(x => x.Role).Returns(LisConnectionRole.Both);
        connection.Setup(x => x.State).Returns(LisConnectionState.Open);
        connectionFactory
            .Setup(x => x.CreateConnection(It.IsAny<int>(), It.IsAny<LisChannel>(), It.IsAny<LisConnectionRole>()))
            .Returns(connection.Object);

        using var connectionPool = new LisConnectionPool(
            optionsMonitor.Object,
            connectionFactory.Object,
            scopeFactory.Object,
            NullLogger<LisConnectionPool>.Instance);

        var sut = new LisConnectionPool.LisSettingsChangedEventHandler(
            connectionPool,
            sender.Object,
            NullLogger<LisConnectionPool.LisSettingsChangedEventHandler>.Instance);

        var oldSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel { Name = "Channel A", ProtocolType = LisProtocolType.Astm },
                new LisChannel { Name = "Channel B", ProtocolType = LisProtocolType.Astm }
            ]
        };
        var newSettings = new LisSettings
        {
            LisChannels = [new LisChannel { Name = "Channel A", ProtocolType = LisProtocolType.Astm }]
        };

        await sut.Handle(new SettingsChangedEvent<LisSettings>(oldSettings, newSettings), TestContext.Current.CancellationToken);

        sender.Verify(
            x => x.Send(
                It.Is<DeleteLisStatisticsCommand>(command => command.ChannelId == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
