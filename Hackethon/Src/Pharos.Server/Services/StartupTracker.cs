using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using MediatR;
using Pharos.Server.Application;

namespace Pharos.Server.Services;

/// <summary>
/// This is a service that tracks the application startup, runs database seeding, and raises <see cref="ApplicationStartedEvent"/>.
/// </summary>
internal class StartupTracker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<StartupTracker> _logger;

    public StartupTracker(
        IServiceProvider services,
        IHostApplicationLifetime lifetime,
        IHostEnvironment environment,
        ILogger<StartupTracker> logger)
    {
        _services = services;
        _lifetime = lifetime;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
        {
            return;
        }

        // We want to stop activity after the application has started and not before app.RunAsync()
        // because we want log events from hosted services to share the same TraceId.
        Program.StartupActivity?.Stop();

        // We want to start a new activity tracing because the startup is complete.
        using var activity = new Activity("Post-startup");
        activity.Start();

        try
        {
            using var scope = _services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await publisher.Publish(new ApplicationStartedEvent(), stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occured while handling {NotificationName}.", typeof(ApplicationStartedEvent).FullName);
        }

        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("Seeding the database with test data.");
            try
            {
                await _services.SeedDatabase();
                _logger.LogInformation("Database seeding complete.");
            }
            catch (ValidationException e)
            {
                _logger.LogError(e, "Error occured while seeding the database with test data.");
                // We want to prevent invalid seeded settings during development
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occured while seeding the database with test data.");
            }
        }
    }

    static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        await using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());
        await using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(
            startedSource.Task,
            cancelledSource.Task).ConfigureAwait(false);

        // If the completed tasks was the "app started" task, return true, otherwise false
        return completedTask == startedSource.Task;
    }
}
