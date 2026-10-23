using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Infrastructure;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Domain;
using Quartz;

namespace Pharos.Server.Lis.Tests.Application.Commands;

public class AskForMissingTestOrdersCommandHandlerTests
{
    private readonly Mock<ISchedulerFactory> _schedulerFactoryMock;
    private readonly Mock<IOptionsSnapshot<LisSettings>> _lisSettingsMock;
    private readonly Mock<ILisMessageBuilder> _messageBuilderMock;
    private readonly Mock<ILisMessageDispatcher> _messageDispatcherMock;
    private readonly Mock<IScheduler> _schedulerMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly ILisMessage _testMessage = Mock.Of<ILisMessage>();
    private readonly AskForMissingTestOrdersCommandHandler _sut;

    public AskForMissingTestOrdersCommandHandlerTests()
    {
        _schedulerFactoryMock = new Mock<ISchedulerFactory>();
        var dateTimeMock = new Mock<IDateTime>();
        _lisSettingsMock = new Mock<IOptionsSnapshot<LisSettings>>();
        _messageBuilderMock = new Mock<ILisMessageBuilder>();
        _messageDispatcherMock = new Mock<ILisMessageDispatcher>();
        _schedulerMock = new Mock<IScheduler>();
        _publisherMock = new Mock<IPublisher>();

        _schedulerFactoryMock.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_schedulerMock.Object);

        dateTimeMock.Setup(x => x.UtcNow)
            .Returns(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _sut = new AskForMissingTestOrdersCommandHandler(
            _schedulerFactoryMock.Object,
            dateTimeMock.Object,
            _lisSettingsMock.Object,
            _messageBuilderMock.Object,
            _messageDispatcherMock.Object,
            _publisherMock.Object);
    }

    [Theory]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5603")]
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
                        AskForMissingRequestsInstrumentScan = !isManualScan,
                        AskForMissingRequestsManualScan = isManualScan,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = false,
                        AskForMissingRequestsManualScan = false,
                        Timeout = 5
                    }
                },
                new LisChannel
                {
                    Name = "Channel3",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = !isManualScan,
                        AskForMissingRequestsManualScan = isManualScan,
                        Timeout = 15
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AskForMissingTestOrdersCommand(sampleIds, isManualScan);

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
    [Trait("ModuleRequirement", "MR_5603")]
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
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 20
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AskForMissingTestOrdersCommand(sampleIds, false);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5603")]
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
                        AskForMissingRequestsInstrumentScan = isManualScan,
                        AskForMissingRequestsManualScan = !isManualScan,
                        Timeout = 5
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = !isManualScan,
                        AskForMissingRequestsManualScan = isManualScan,
                        Timeout = 10
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);

        var command = new AskForMissingTestOrdersCommand(sampleIds, isManualScan);

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
    [Trait("ModuleRequirement", "MR_5603")]
    public async Task Handle_Should_NotScheduleJob_When_JobAlreadyExists()
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
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 10
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.Is<JobKey>(k => k.Name == "publish-missing-test-orders-key-S001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schedulerMock.Setup(x => x.CheckExists(It.Is<JobKey>(k => k.Name == "publish-missing-test-orders-key-S002"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AskForMissingTestOrdersCommand(sampleIds, false);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.Is<IJobDetail>(j => j.Key.Name == "publish-missing-test-orders-key-S001"),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.Is<IJobDetail>(j => j.Key.Name == "publish-missing-test-orders-key-S002"),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5603")]
    public async Task Handle_Should_ScheduleJobOnlyOnce_When_DuplicateSampleIdsProvided()
    {
        var sampleIds = new[] { "S001", "S001", "S002" };
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
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 10
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AskForMissingTestOrdersCommand(sampleIds, false);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.Is<IJobDetail>(j => j.Key.Name == "publish-missing-test-orders-key-S001"),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _schedulerMock.Verify(x => x.ScheduleJob(
            It.Is<IJobDetail>(j => j.Key.Name == "publish-missing-test-orders-key-S002"),
            It.IsAny<ITrigger>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5853")]
    [Trait("ProductRequirement", "PR_1796")]
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
                        AskForMissingRequestsManualScan = true,
                        AskForMissingRequestsInstrumentScan = false,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsManualScan = false,
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 20
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var commandInstrumentScan = new AskForMissingTestOrdersCommand(sampleIds, false);
        await _sut.Handle(commandInstrumentScan, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel2"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);

        _messageBuilderMock.Reset();
        _messageDispatcherMock.Reset();
        _schedulerMock.Reset();

        _schedulerFactoryMock.Setup(x => x.GetScheduler(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_schedulerMock.Object);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var commandManualScan = new AskForMissingTestOrdersCommand(sampleIds, true);
        await _sut.Handle(commandManualScan, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_PublishMissingTestOrdersEvent_When_AllImportSettingsAreDisabled()
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
                        PanelExpansion = false,
                        ManualQueryForAll = false,
                        ManualRefreshExistingRequests = false,
                        AskForMissingRequestsInstrumentScan = false,
                        RefreshExistingRequestsInstrumentScan = false,
                        AskForMissingRequestsManualScan = false,
                        RefreshExistingRequestsManualScan = false
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);

        var command = new AskForMissingTestOrdersCommand(sampleIds, false);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _publisherMock.Verify(x => x.Publish(
            It.Is<MissingTestOrdersEvent>(e => e.SampleId == "S001"),
            It.IsAny<CancellationToken>()), Times.Once);

        _publisherMock.Verify(x => x.Publish(
            It.Is<MissingTestOrdersEvent>(e => e.SampleId == "S002"),
            It.IsAny<CancellationToken>()), Times.Once);

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
    [Trait("ModuleRequirement", "MR_5603")]
    public async Task Handle_Should_SendQueryToChannels_When_AtLeastOneImportSettingIsEnabled()
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
                        PanelExpansion = true,
                        ManualQueryForAll = false,
                        ManualRefreshExistingRequests = false,
                        AskForMissingRequestsInstrumentScan = true,
                        RefreshExistingRequestsInstrumentScan = false,
                        AskForMissingRequestsManualScan = false,
                        RefreshExistingRequestsManualScan = false,
                        Timeout = 10
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AskForMissingTestOrdersCommand(sampleIds, false);

        await _sut.Handle(command, TestContext.Current.CancellationToken);

        _messageBuilderMock.Verify(x => x.BuildRequestMessage(
            It.Is<LisChannel>(c => c.Name == "Channel1"),
            It.IsAny<IReadOnlyList<string>>()), Times.Once);

        _messageDispatcherMock.Verify(x => x.PostMessage(
            It.IsAny<int>(),
            It.IsAny<LisMessagePriority>(),
            It.IsAny<LisMessageType>(),
            It.IsAny<Dictionary<string, string>>(),
            It.IsAny<ILisMessage>()), Times.Once);

        _publisherMock.Verify(x => x.Publish(
            It.IsAny<MissingTestOrdersEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_1796")]
    [Trait("ModuleRequirement", "MR_5603")]
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
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 5
                    }
                },
                new LisChannel
                {
                    Name = "Channel1",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 10
                    }
                },
                new LisChannel
                {
                    Name = "Channel2",
                    IsEnabled = false,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 15
                    }
                },
                new LisChannel
                {
                    Name = "Channel3",
                    IsEnabled = true,
                    ImportSettings = new LisImportSettings
                    {
                        AskForMissingRequestsInstrumentScan = true,
                        Timeout = 20
                    }
                }
            ]
        };

        _lisSettingsMock.Setup(x => x.Value).Returns(lisSettings);
        _messageBuilderMock.Setup(x => x.BuildRequestMessage(It.IsAny<LisChannel>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(_testMessage);
        _schedulerMock.Setup(x => x.CheckExists(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AskForMissingTestOrdersCommand(sampleIds, false);

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
