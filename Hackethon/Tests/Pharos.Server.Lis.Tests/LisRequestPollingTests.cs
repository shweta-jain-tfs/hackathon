using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Lis.Application.Commands;
using Pharos.Server.Lis.Application.Services;

namespace Pharos.Server.Lis.Tests;

public class LisRequestPollingTests : IDisposable
{
    private const int _pollingFrequencyMilliseconds = 50; // Reduced frequency for testing

    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ISender> _mockSender;
    private readonly Mock<IOptionsMonitor<LisSettings>> _mockLisSettings;
    private readonly ILogger<LisRequestPolling> _logger;
    private LisRequestPolling? _polling;

    public LisRequestPollingTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        var mockServiceScope = new Mock<IServiceScope>();
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockSender = new Mock<ISender>();
        _mockLisSettings = new Mock<IOptionsMonitor<LisSettings>>();
        _logger = NullLogger<LisRequestPolling>.Instance;

        // Setup service provider chain
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        mockServiceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(mockServiceScope.Object);

        mockServiceScope
            .Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(ISender)))
            .Returns(_mockSender.Object);

        // Setup default LisSettings with channels
        SetupLisSettings(CreateLisSettings(3));
    }

    private void SetupLisSettings(LisSettings settings)
    {
        _mockLisSettings.Setup(x => x.CurrentValue).Returns(settings);
    }

    private static LisSettings CreateLisSettings(int channelCount, bool pollForRequests = true, int frequency = _pollingFrequencyMilliseconds)
    {
        var settings = new LisSettings();
        for (var i = 0; i < channelCount; i++)
        {
            settings.LisChannels.Add(new LisChannel
            {
                Name = $"Channel {i}",
                ImportSettings = new LisImportSettings(intervalsAreMilliseconds: true)
                {
                    PollForRequests = pollForRequests,
                    Frequency = frequency
                }
            });
        }
        return settings;
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Assert
        _polling.Should().NotBeNull();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void IsPollingForChannel_WhenNotStarted_ShouldReturnFalse()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act & Assert
        _polling.IsPollingForChannel(1).Should().BeFalse();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Start_ShouldStartPollingAndReturnTrueForIsPolling()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);
        const int channelId = 1;

        // Act
        _polling.Start(channelId);

        // Assert
        _polling.IsPollingForChannel(channelId).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Start_WithMultipleChannels_ShouldTrackAllChannels()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act
        _polling.Start(0);
        _polling.Start(1);

        // Assert
        _polling.IsPollingForChannel(0).Should().BeTrue();
        _polling.IsPollingForChannel(1).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Stop_ShouldStopPollingForSpecificChannel()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);
        _polling.Start(0);
        _polling.Start(1);

        // Act
        _polling.Stop(0);

        // Assert
        _polling.IsPollingForChannel(0).Should().BeFalse();
        _polling.IsPollingForChannel(1).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Stop_WhenNotActive_ShouldNotThrow()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act & Assert
        _polling.Invoking(p => p.Stop(1)).Should().NotThrow();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void StopAll_ShouldStopAllActiveChannels()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);
        _polling.Start(0);
        _polling.Start(1);
        _polling.Start(2);

        // Act
        _polling.StopAll();

        // Assert
        _polling.IsPollingForChannel(0).Should().BeFalse();
        _polling.IsPollingForChannel(1).Should().BeFalse();
        _polling.IsPollingForChannel(2).Should().BeFalse();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task Start_ShouldInvokePollingWithCorrectCommand()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<bool>();
        SendAllTestOrderRequestsToLisCommand? capturedCommand = null;

        _mockSender
            .Setup(x => x.Send(It.IsAny<SendAllTestOrderRequestsToLisCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((cmd, _) =>
            {
                capturedCommand = cmd as SendAllTestOrderRequestsToLisCommand;
                taskCompletionSource.TrySetResult(true);
            })
            .Returns(Task.CompletedTask);

        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);
        const int channelId = 0;

        // Act
        _polling.Start(channelId);
        await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.LisChannelId.Should().Be(channelId);
        _mockSender.Verify(
            x => x.Send(It.IsAny<SendAllTestOrderRequestsToLisCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5602")]
    [Trait("ProductRequirement", "PR_1796")]
    public async Task PollForRequestsAsync_WhenExceptionThrown_ShouldContinuePolling()
    {
        // Arrange
        var callCount = 0;
        var secondCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _mockSender
            .Setup(x => x.Send(It.IsAny<SendAllTestOrderRequestsToLisCommand>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref callCount) >= 2)
                {
                    secondCall.TrySetResult();
                }
            })
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act
        _polling.Start(0);
        var completed = await Task.WhenAny(secondCall.Task, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        // Assert
        completed.Should().Be(secondCall.Task, "polling should continue after an exception");
        _polling.IsPollingForChannel(0).Should().BeTrue();
    }

    [Theory]
    [Trait("ModuleRequirement", "MR_5602")]
    [Trait("ProductRequirement", "PR_1796")]
    [InlineData(1)]
    [InlineData(5)]
    public void Start_WithDifferentFrequencies_ShouldNotThrow(int frequency)
    {
        // Arrange
        SetupLisSettings(CreateLisSettings(3, true, frequency));
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act & Assert
        _polling.Invoking(p => p.Start(0)).Should().NotThrow();
        _polling.IsPollingForChannel(0).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Start_AfterStop_ShouldRestartPolling()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);
        _polling.Start(0);
        _polling.Stop(0);

        // Act
        _polling.Start(0);

        // Assert
        _polling.IsPollingForChannel(0).Should().BeTrue();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Start_WhenPollForRequestsIsFalse_ShouldNotStartPolling()
    {
        // Arrange
        SetupLisSettings(CreateLisSettings(3, pollForRequests: false));
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act
        _polling.Start(0);

        // Assert
        _polling.IsPollingForChannel(0).Should().BeFalse();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5602")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Start_WhenFrequencyIsZero_ShouldNotStartPolling()
    {
        // Arrange
        SetupLisSettings(CreateLisSettings(3, pollForRequests: true, frequency: 0));
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);

        // Act
        _polling.Start(0);

        // Assert
        _polling.IsPollingForChannel(0).Should().BeFalse();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5601")]
    [Trait("ProductRequirement", "PR_1796")]
    public void Start_WhenAlreadyPolling_ShouldNotStartDuplicateTimer()
    {
        // Arrange
        _polling = new LisRequestPolling(_mockServiceProvider.Object, _logger, _mockLisSettings.Object);
        _polling.Start(0);

        // Act
        _polling.Start(0); // Start again

        // Assert
        _polling.IsPollingForChannel(0).Should().BeTrue();
    }

    public void Dispose()
    {
        // Dispose all polling timers after each test to prevent callbacks
        // from interfering with subsequent tests.
        _polling?.StopAll();
    }
}
