using System.Net.WebSockets;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pharos.Client.App.Identity;
using Pharos.Client.App.Identity.Services;
using Pharos.Client.Common.App.Services;
using Pharos.Client.Common.Models;
using Pharos.Client.Common.Models.DataServices;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Client.Common.Models.Services;
using Pharos.Contracts.Notifications;
using Pharos.Contracts;

namespace Pharos.Client.Tests.Services;

public class AutoLogoutReconnectTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Reconnect_ReloadsMissedSettingsWithoutResettingUnchangedDeadline(bool initiallyEnabled, bool enabledAfterReconnect)
    {
        using var testCts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        testCts.CancelAfter(TimeSpan.FromSeconds(15));
        var token = testCts.Token;
        var probe = new ConnectionProbe();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton(probe);
        await using var app = builder.Build();
        app.MapHub<ReconnectHub>("/" + HubRoutes.CurrentUser);
        await app.StartAsync(token);

        var reconnectAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowReconnect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        WebSocket? socket = null;
        await using var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/" + HubRoutes.CurrentUser, options =>
            {
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.WebSocketFactory = async (context, cancellationToken) =>
                {
                    if (Interlocked.Increment(ref attempts) > 1)
                    {
                        reconnectAttempt.TrySetResult();
                        await allowReconnect.Task.WaitAsync(cancellationToken);
                    }
                    socket = await app.GetTestServer().CreateWebSocketClient().ConnectAsync(context.Uri, cancellationToken);
                    return socket;
                };
            })
            .WithAutomaticReconnect([TimeSpan.Zero])
            .Build();
        var factory = new Mock<IHubConnectionFactory>();
        factory.Setup(x => x.CreateConnection(HubRoutes.CurrentUser)).Returns(connection);
        var services = new ServiceCollection();
        services.AddSingleton(factory.Object);
        IdentityModule.Instance.AddServices(services);
        using var provider = services.BuildServiceProvider();
        var notifications = provider.GetRequiredService<ICurrentUserNotificationProvider>();
        var instrumentNotifications = new Mock<ICurrentInstrumentNotificationProvider>();
        var enabled = initiallyEnabled;
        var reads = 0;
        var source = new Mock<ISingletonDataSource<UiSettings>>();
        source.Setup(x => x.GetAsync()).ReturnsAsync(() =>
        {
            var settings = new UiSettings
            {
                SecuritySettings = new SecuritySettings { AutoLogoutEnabled = enabled, AutoLogoutTimeoutMinutes = 10 }
            };
            Interlocked.Increment(ref reads);
            return settings;
        });
        var device = new Mock<IClientDeviceService>();
        device.SetupGet(x => x.ClientDeviceType).Returns(ClientDeviceType.Instrument);
        var navigation = new AutoLogoutNavigationManager();
        using var sut = new AutoLogoutService(new UiSettingsService(source.Object), notifications, instrumentNotifications.Object,
            new TestAuthenticationStateProvider(), device.Object, navigation, NullLogger<AutoLogoutService>.Instance);
        int? countdown = null;
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.UserIdle += seconds => { countdown = seconds; return Task.CompletedTask; };
        sut.UserActive += () =>
        {
            countdown = null;
            dismissed.TrySetResult();
            return Task.CompletedTask;
        };
        await sut.StartAsync();
        await sut.StartLogoutCountdownAsync();
        var originalSecondsRemaining = countdown;
        await probe.Subscriptions.Reader.ReadAsync(token);
        var settingsApplied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        notifications.AutoLogoutSettingsChanged += () =>
        {
            // The preceding auto-logout handler completes synchronously with this in-memory data source.
            settingsApplied.TrySetResult();
            return Task.CompletedTask;
        };

        socket!.Abort();
        await reconnectAttempt.Task.WaitAsync(token);
        enabled = enabledAfterReconnect;
        allowReconnect.SetResult();
        await probe.Subscriptions.Reader.ReadAsync(token);
        await settingsApplied.Task.WaitAsync(token);
        if (initiallyEnabled && !enabledAfterReconnect)
        {
            await dismissed.Task.WaitAsync(token);
        }
        countdown.Should().Be(initiallyEnabled && enabledAfterReconnect ? originalSecondsRemaining : null);
        sut.StayLoggedIn();
        await sut.HandleTimeoutAsync();

        reads.Should().Be(2);
        if (enabledAfterReconnect)
        {
            await navigation.Navigated.Task.WaitAsync(token);
        }
        (navigation.LastUri is not null).Should().Be(enabledAfterReconnect);
    }

    public sealed class ConnectionProbe
    {
        public Channel<HubCallerContext> Subscriptions { get; } = Channel.CreateUnbounded<HubCallerContext>();
    }

    public sealed class ReconnectHub(ConnectionProbe probe) : Hub
    {
        public Task Subscribe()
        {
            return probe.Subscriptions.Writer.WriteAsync(Context).AsTask();
        }
    }
}
