using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Pharos.Server.Integration.Tests.Common;

public abstract class DatabaseTestBase<TWaf>(TWaf factory) : IAsyncLifetime where TWaf : DatabaseTestsWaf
{
    private AsyncServiceScope? _scope;

    protected TWaf Factory { get; } = factory;

    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    public virtual async ValueTask InitializeAsync()
    {
        _scope = Factory.Services.CreateAsyncScope();
        ServiceProvider = _scope.Value.ServiceProvider;
        await Factory.InitializeRespawner();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_scope.HasValue)
        {
            await _scope.Value.DisposeAsync();
            _scope = null;
        }

        try
        {
            await WaitForNotificationsAsync();
        }
        catch (TimeoutException)
        {
            // Timed out waiting for notifications; reset the notification state so
            // stale signals don't cascade into subsequent tests.
            var backgroundNotificationHandler = Factory.Services.GetRequiredService<IBackgroundNotificationHandler>();
            backgroundNotificationHandler.ResetNotificationCount();
        }
        finally
        {
            // Always reset the database state, even if notification waiting times out,
            // to prevent dirty state from cascading into subsequent tests.
            await Factory.ResetState();
        }
    }

    /// <summary>
    /// Wait for background notifications to be processed
    /// </summary>
    protected Task WaitForNotificationsAsync()
    {
        var backgroundNotificationHandler = Factory.Services.GetRequiredService<IBackgroundNotificationHandler>();
        return backgroundNotificationHandler.WaitForNotificationsAsync(TimeSpan.FromSeconds(5)); // if it is not done in 5 seconds, something is wrong
    }
}