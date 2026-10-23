using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Identity.Infrastructure.Database;
using Pharos.Server.Infrastructure.Settings;
using Pharos.Server.Instrument.Infrastructure.Database;
using Pharos.Server.Lis.Application.Interfaces;
using Pharos.Server.Lis.Infrastructure.Database;
using Pharos.Server.Parameters.Infrastructure.Database;
using Pharos.Server.Reagents.Infrastructure.Database;
using Pharos.Server.Request.Infrastructure.Database;
using Pharos.Server.Result.Infrastructure.Database;
using Pharos.Server.Sample.Infrastructure.Database;
using Pharos.Server.Settings.Infrastructure.Database;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace Pharos.Server.Integration.Tests.Common;

public abstract class DatabaseTestsWaf : WebApplicationFactory<Program>, IAsyncLifetime
{
    private Respawner? _respawner;

    protected abstract string ConnectionStringName { get; }

    public string ConnectionString
    {
        get
        {
            if (string.IsNullOrEmpty(field))
            {
                field = ResolveConnectionStringFromEnvironment();

                if (string.IsNullOrEmpty(field))
                {
                    var error = @$"The environment variable 'ConnectionStrings__{ConnectionStringName}' with the database connection string is not set.
Refer to to the solution README.md for instruction on how to fix this issue.";
                    throw new InvalidOperationException(error);
                }
            }
            return field;
        }
    }

    private string? ResolveConnectionStringFromEnvironment()
    {
        var keys = new[]
        {
            $"ConnectionStrings__{ConnectionStringName}",
            $"ConnectionStrings:{ConnectionStringName}",
            ConnectionStringName
        };

        foreach (var key in keys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return null;
    }

    public virtual ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public virtual async Task InitializeRespawner()
    {
        if (_respawner != null)
        {
            return;
        }

        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
            {
                // Only migration history tables are ignored — they must never be deleted.
                // IMPORTANT: The parameter import in ApplicationStartedEventHandler must not run during tests.
                // If it does (e.g. a ParameterUpdatePackage*.zip exists on the machine), it seeds data across
                // many Parameters tables on every WAF startup. The Parameters schema has a complex FK graph,
                // and Respawner cannot fully delete that seeded data — FK violations cause the reset to fail,
                // leaving the database dirty and causing cascading test failures.
                // Tests that need Parameters data must insert it themselves.
                TablesToIgnore =
                [
                    new Table(InstrumentDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(LisDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(ParametersDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(ReagentsDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(RequestDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(ResultDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(IdentityDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(SettingsDbContext.Schema, HistoryRepository.DefaultTableName),
                    new Table(SampleDbContext.Schema, HistoryRepository.DefaultTableName)
                ],
                WithReseed = true,
                DbAdapter = DbAdapter.SqlServer
            });
        }
        catch
        {
            // If the test database is not created yet, we get an error trying to initialize Respawner.
            // No database also means that we don't need to reset it.
        }
    }

    public virtual async Task ResetState()
    {
        if (_respawner != null)
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            try
            {
                await _respawner.ResetAsync(conn);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Database reset failed. The test database may be dirty and later test failures may be cascading from this failure.",
                    ex);
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        base.ConfigureWebHost(builder);

        builder.UseSetting($"{DatabaseSettings.ConnectionStringName}", ConnectionStringName);
        builder.UseSetting($"ConnectionStrings:{ConnectionStringName}", ConnectionString);
        builder.ConfigureServices(services =>
        {
            services.ReplaceSingletonService<IBackgroundNotificationHandler, BackgroundNotificationHandler>(sp => new BackgroundNotificationHandler(sp.GetRequiredService<IServiceScopeFactory>(), testMode: true));

            // Prevent LisConnectionPool from attempting real TCP connections in all test WAFs.
            // Running in Development mode seeds an enabled LIS channel by default (see LisSettings.InitializeConfigSection).
            // IntegrationTestsWaf overrides this with a more specific mock for tests that need LIS interaction.
            var factoryMock = new Mock<ILisConnectionFactory>();
            factoryMock
                .Setup(x => x.CreateConnection(It.IsAny<int>(), It.IsAny<LisChannel>(), It.IsAny<LisConnectionRole>()))
                .Returns(Mock.Of<ILisConnection>());
            services.RemoveAllKeyed<ILisConnectionFactory>(LisServiceKeys.AstmConnectionFactory);
            services.AddKeyedSingleton<ILisConnectionFactory>(LisServiceKeys.AstmConnectionFactory, factoryMock.Object);
        });
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await base.DisposeAsync();
    }
}