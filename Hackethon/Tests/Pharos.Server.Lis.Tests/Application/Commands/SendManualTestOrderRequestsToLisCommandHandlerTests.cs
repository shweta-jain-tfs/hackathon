using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Lis.Tests.Application.Commands;

public class SendManualTestOrderRequestsToLisCommandHandlerTests
{
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock;
    private readonly Mock<ILisMessageBuilder> _messageBuilderMock;
    private readonly Mock<ILisMessageDispatcher> _messageDispatcherMock;
    private readonly ILisMessage _testMessage = Mock.Of<ILisMessage>();
    private readonly SendManualTestOrderRequestsToLisCommandHandler _sut;

    public SendManualTestOrderRequestsToLisCommandHandlerTests()
    {
        _lisSettingsMock = new Mock<IOptionsSnapshot<LisSettings>>();
        _messageBuilderMock = new Mock<ILisMessageBuilder>();
        _messageDispatcherMock = new Mock<ILisMessageDispatcher>();
        _sut = new SendManualTestOrderRequestsToLisCommandHandler(
            _lisSettingsMock.Object,
            _messageBuilderMock.Object,
            _messageDispatcherMock.Object);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5608")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Handle_Should_SendQueryToManualQueryForAllEnabledChannels_When_SampleIdsIsNull()
    {
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
                        ManualQueryForAll = true
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        ManualQueryForAll = false
                    }
                },
                new LisChannel
                {
                    Name = "Channel3",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        ManualQueryForAll = true
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new SendManualTestOrderRequestsToLisCommand(null);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
            It.Is<IReadOnlyList<string>>(s => s.Count == 0)), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            0,
            LisMessagePriority.High,
            LisMessageType.TestOrderQuery,
            new(),
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
    [Trait("ModuleRequirement", "MR_5609")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Handle_Should_SendQueryToManualRefreshExistingRequestsEnabledChannels_When_SampleIdsProvided()
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
                        ManualRefreshExistingRequests = true
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        ManualRefreshExistingRequests = false
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new SendManualTestOrderRequestsToLisCommand(sampleIds);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
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
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5608")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Handle_Should_SendQueryToMultipleChannels_When_MultipleSampleIdsProvided()
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
                        ManualRefreshExistingRequests = true
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        ManualRefreshExistingRequests = true
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new SendManualTestOrderRequestsToLisCommand(sampleIds);

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

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Handle_Should_SendSeparateQueriesToHl7Channel_When_MultipleSampleIdsProvided()
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
                    ProtocolType = LisProtocolType.HL7,
                    ImportSettings = new LisImportSettings
                    {
                        ManualRefreshExistingRequests = true
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new SendManualTestOrderRequestsToLisCommand(sampleIds);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        foreach (var sampleId in sampleIds)
        {
            _messageBuilderMock.Verify(x => x.BuildRequestMessage(
                It.Is<LisChannel>(c => c.Name == "Channel1"),
                It.Is<IReadOnlyList<string>>(s => s.Count == 1 && s[0] == sampleId)), Times.Once);

            _messageDispatcherMock.Verify(x => x.PostMessage(
                0,
                LisMessagePriority.High,
                LisMessageType.TestOrderQuery,
                It.Is<Dictionary<string, string>>(m => m[LisMessageMetadataKeys.SampleIds] == sampleId),
                _testMessage), Times.Once);
        }

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.IsAny<LisChannel>(),
            It.IsAny<IReadOnlyList<string>>()), Times.Exactly(sampleIds.Length));

        _messageDispatcherMock.Verify(x => x.PostMessage(
            It.IsAny<int>(),
            It.IsAny<LisMessagePriority>(),
            It.IsAny<LisMessageType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<ILisMessage>()), Times.Exactly(sampleIds.Length));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5608")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Handle_Should_NotSendQuery_When_NoEnabledChannelsWithManualQueryForAll()
    {
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
                        ManualQueryForAll = false
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        ManualQueryForAll = true
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);

        var command = new SendManualTestOrderRequestsToLisCommand(null);

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
    [Trait("ModuleRequirement", "MR_5609")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Handle_Should_NotSendQuery_When_NoEnabledChannelsWithManualRefreshExistingRequests()
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
                        ManualRefreshExistingRequests = false
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);

        var command = new SendManualTestOrderRequestsToLisCommand(sampleIds);

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
    [Trait("ModuleRequirement", "MR_5609")]
    [Trait("ProductRequirement", "PR_1796")]
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
                        ManualRefreshExistingRequests = true
                    }
                },
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        ManualRefreshExistingRequests = true
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        ManualRefreshExistingRequests = true
                    }
                },
                new LisChannel
                {
                    Name = "Channel3",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        ManualRefreshExistingRequests = true
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);

        var command = new SendManualTestOrderRequestsToLisCommand(sampleIds);

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
