using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Application.Queries;

namespace Pharos.Server.Lis.Tests.Application.Queries;

[Trait("ProductRequirement", "PR_564")]
[Trait("ModuleRequirement", "MR_5850")]
public class GetLisQueryHandlerTests
{
    private readonly Mock<ILisConnectionPool> _connectionPoolMock;
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock;
    private readonly GetLisQueryHandler _sut;

    public GetLisQueryHandlerTests()
    {
        _connectionPoolMock = new Mock<ILisConnectionPool>();
        _lisSettingsMock = new Mock<IOptionsSnapshot<LisSettings>>();
        _sut = new GetLisQueryHandler(_connectionPoolMock.Object, _lisSettingsMock.Object);
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5379")]
    [Fact]
    public async Task Handle_Should_ReturnMappedDtos_When_ChannelsConfigured()
    {
        // Arrange
        var lisSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel { Name = "Channel A" },
                new LisChannel { Name = "Channel B" }
            ]
        };
        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _connectionPoolMock
            .Setup(x => x.GetConnectionStates(0))
            .Returns([new LisConnectionStatus(LisConnectionRole.Both, LisConnectionState.Open)]);
        _connectionPoolMock
            .Setup(x => x.GetConnectionStates(1))
            .Returns([new LisConnectionStatus(LisConnectionRole.Both, LisConnectionState.Closed)]);



        // Act
        var result = await _sut.Handle(new GetLisQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new { Id = 0, State = LisConnectionState.Open });
        result[1].Should().BeEquivalentTo(new { Id = 1, State = LisConnectionState.Closed });
        _connectionPoolMock.Verify(x => x.GetConnectionStates(0), Times.Once);
        _connectionPoolMock.Verify(x => x.GetConnectionStates(1), Times.Once);
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5379")]
    [Fact]
    public async Task Handle_Should_ReturnClosed_When_MainPortOpenAndExportPortDisconnected()
    {
        // Arrange
        var lisSettings = new LisSettings
        {
            LisChannels = [new LisChannel { Name = "Channel A" }]
        };
        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _connectionPoolMock
            .Setup(x => x.GetConnectionStates(0))
            .Returns([
                new LisConnectionStatus(LisConnectionRole.Import, LisConnectionState.Open),
                new LisConnectionStatus(LisConnectionRole.Export, LisConnectionState.Closed)
            ]);

        // Act
        var result = await _sut.Handle(new GetLisQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].State.Should().NotBe(LisConnectionState.Open);
    }

    [Trait("ProductRequirement", "PR_2134")]
    [Trait("ModuleRequirement", "MR_5379")]
    [Fact]
    public async Task Handle_Should_ReturnOpen_When_BothPortsOpen()
    {
        // Arrange
        var lisSettings = new LisSettings
        {
            LisChannels = [new LisChannel { Name = "Channel A" }]
        };
        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _connectionPoolMock
            .Setup(x => x.GetConnectionStates(0))
            .Returns([
                new LisConnectionStatus(LisConnectionRole.Import, LisConnectionState.Open),
                new LisConnectionStatus(LisConnectionRole.Export, LisConnectionState.Open)
            ]);

        // Act
        var result = await _sut.Handle(new GetLisQuery(), TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(1);
        result[0].State.Should().Be(LisConnectionState.Open);
    }
}
