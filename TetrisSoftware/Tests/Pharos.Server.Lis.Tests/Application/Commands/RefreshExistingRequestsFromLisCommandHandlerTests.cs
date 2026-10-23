using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Tests.Application.Commands;

public class RefreshExistingRequestsFromLisCommandHandlerTests
{
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock;
    private readonly Mock<ILisMessageBuilder> _messageBuilderMock;
    private readonly Mock<ILisMessageDispatcher> _messageDispatcherMock;
    private readonly ILisMessage _testMessage = Mock.Of<ILisMessage>();
    private readonly RefreshExistingRequestsCommandHandler _sut;

    public RefreshExistingRequestsFromLisCommandHandlerTests()
    {
        _lisSettingsMock = new Mock<IOptionsSnapshot<LisSettings>>();
        _messageBuilderMock = new Mock<ILisMessageBuilder>();
        _messageDispatcherMock = new Mock<ILisMessageDispatcher>();

        _sut = new RefreshExistingRequestsCommandHandler(
            _lisSettingsMock.Object,
            _messageBuilderMock.Object,
            _messageDispatcherMock.Object);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5606")]
    [InlineData(false, "Channel1")]
    [InlineData(true, "Channel1")]
    public async Task Handle_Should_SendQueryToEnabledChannels_When_ChannelsConfiguredForScanType(bool isManualScan, string expectedChannelName)
    {
        var sampleIds = new[] { "S001", "S002" };
        var lisSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = !isManualScan,
                        RefreshExistingRequestsManualScan = isManualScan,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = false,
                        RefreshExistingRequestsManualScan = false,
                        Timeout = 5
                    }
                },
                new LisChannel
                {
                    Name = "Channel3",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = !isManualScan,
                        RefreshExistingRequestsManualScan = isManualScan,
                        Timeout = 15
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new RefreshExistingRequestsCommand(sampleIds, isManualScan);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == expectedChannelName),
            It.Is<IReadOnlyList<string>>(s => s.Count == 2 && s[0] == "S001" && s[1] == "S002")), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            0,
            LisMessagePriority.High,
            LisMessageType.TestOrderQuery,
            It.IsAny<Dictionary<string, string>>(),
            _testMessage), Times.Once);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.IsAny<LisChannel>(),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            It.IsAny<int>(),
            It.IsAny<LisMessagePriority>(),
            It.IsAny<LisMessageType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<ILisMessage>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5606")]
    public async Task Handle_Should_SendQueryToMultipleChannels_When_MultipleChannelsEnabled()
    {
        var sampleIds = new[] { "S001", "S002", "S003" };
        var lisSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 20
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new RefreshExistingRequestsCommand(sampleIds, false);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
            It.Is<IReadOnlyList<string>>(s => s.Count == 3)), Times.Once);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel2"),
            It.Is<IReadOnlyList<string>>(s => s.Count == 3)), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            0,
            LisMessagePriority.High,
            LisMessageType.TestOrderQuery,
            It.IsAny<Dictionary<string, string>>(),
            _testMessage), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            1,
            LisMessagePriority.High,
            LisMessageType.TestOrderQuery,
            It.IsAny<Dictionary<string, string>>(),
            _testMessage), Times.Once);
    }

    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5606")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_Should_NotSendQuery_When_NoEnabledChannelsForScanType(bool isManualScan)
    {
        var sampleIds = new[] { "S001" };
        var lisSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = isManualScan,
                        RefreshExistingRequestsManualScan = !isManualScan,
                        Timeout = 5
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = !isManualScan,
                        RefreshExistingRequestsManualScan = isManualScan,
                        Timeout = 10
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);

        var command = new RefreshExistingRequestsCommand(sampleIds, isManualScan);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.IsAny<LisChannel>(),
            It.IsAny<IReadOnlyList<string>>()), Times.Never);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            It.IsAny<int>(),
            It.IsAny<LisMessagePriority>(),
            It.IsAny<LisMessageType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<ILisMessage>()), Times.Never);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5854")]
    public async Task Handle_Should_UseCorrectSetting_When_BothManualAndInstrumentScanAreConfigured()
    {
        var sampleIds = new[] { "S001" };
        var lisSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsManualScan = true,
                        RefreshExistingRequestsInstrumentScan = false,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsManualScan = false,
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 20
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var commandInstrumentScan = new RefreshExistingRequestsCommand(sampleIds, false);
        await _sut.Handle(commandInstrumentScan, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel2"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);

        _messageBuilderMock.Reset();
        _messageDispatcherMock.Reset();

        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var commandManualScan = new RefreshExistingRequestsCommand(sampleIds, true);
        await _sut.Handle(commandManualScan, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5606")]
    public async Task Handle_Should_UseCorrectChannelIndices_When_DisabledChannelsAreMixedWithEnabledOnes()
    {
        var sampleIds = new[] { "S001", "S002" };
        var lisSettings = new LisSettings
        {
            LisChannels =
            [
                new LisChannel
                {
                    Name = "Channel0",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 5
                    }
                },
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 15
                    }
                },
                new LisChannel
                {
                    Name = "Channel3",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        RefreshExistingRequestsInstrumentScan = true,
                        Timeout = 20
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new RefreshExistingRequestsCommand(sampleIds, false);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel3"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            1,
            LisMessagePriority.High,
            LisMessageType.TestOrderQuery,
            It.IsAny<Dictionary<string, string>>(),
            _testMessage), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            3,
            LisMessagePriority.High,
            LisMessageType.TestOrderQuery,
            It.IsAny<Dictionary<string, string>>(),
            _testMessage), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            0,
            It.IsAny<LisMessagePriority>(),
            It.IsAny<LisMessageType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<ILisMessage>()), Times.Never);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            2,
            It.IsAny<LisMessagePriority>(),
            It.IsAny<LisMessageType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<ILisMessage>()), Times.Never);
    }
}
