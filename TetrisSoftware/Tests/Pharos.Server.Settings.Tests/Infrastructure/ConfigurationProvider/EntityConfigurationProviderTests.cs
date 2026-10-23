using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Infrastructure.Settings;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.Database;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class EntityConfigurationProviderTests
{
    private const string _defaultKey1 = "Section1:Option1";
    private const string _defaultKey2 = "Section1:Option2";
    private const string _defaultKey3 = "Section2:Option1";
    private const string _defaultValue1 = "Value1";
    private const string? _defaultValue2 = null;
    private const string _defaultValue3 = "Value3";
    private const string _currentValue1 = "CurrentValue1";
    private const string _currentValue2 = "CurrentValue2";
    private const string _dbKey1 = "Section2:Option123";
    private const string _dbValue1 = "Value4";

    private readonly Dictionary<string, string?> _defaultSettings = new()
    {
        { _defaultKey1, _defaultValue1 },
        { _defaultKey2, _defaultValue2 },
        { _defaultKey3, _defaultValue3 },
    };

    private readonly Mock<IConfigurationSource> _configSourceMock;
    private readonly WebApplication _app;
    private readonly IConfiguration _configuration;

    public EntityConfigurationProviderTests()
    {
        EntityConfigurationProvider.Instance = null;
        _configSourceMock = new Mock<IConfigurationSource>();
        _configSourceMock
            .Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
            .Returns(() => EntityConfigurationProvider.Create(_defaultSettings));

        var appBuilder = WebApplication.CreateBuilder();
        var configBuilder = (IConfigurationBuilder)appBuilder.Configuration;
        configBuilder.Add(_configSourceMock.Object);

        appBuilder.Configuration["ConnectionStrings:" + DatabaseSettings.MainConnectionStringName] = null; // to use in-memory database for tests
        SettingsModule.Instance.AddServices(appBuilder.Services, appBuilder.Configuration, null);
        appBuilder.Services.AddSignalR();
        _app = appBuilder.Build();
        _configuration = _app.Configuration;
    }

    [Fact]
    public void DefaultSettingsAreAddedToConfigurationWhenProviderIsBuilt()
    {
        EntityConfigurationProvider.Instance = null;
        using var configuration = new ConfigurationManager();
        configuration.GetChildren().Should().HaveCount(0);

        ((IConfigurationBuilder)configuration).Add(_configSourceMock.Object);

        configuration.GetChildren().Should().HaveCount(2); // two sections
        configuration[_defaultKey1].Should().Be(_defaultValue1);
        configuration[_defaultKey2].Should().Be(_defaultValue2);
        configuration[_defaultKey3].Should().Be(_defaultValue3);
    }

    [Fact]
    public async Task CurrentSettingsLoadedFromDatabaseWhenSettingsModuleIsFullyInitialized()
    {
        using var dbContext = _app.Services.GetRequiredService<ISettingsDbContext>();

        await ((SettingsDbContext)dbContext).Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        try
        {
            dbContext.Settings.Add(new Setting { Key = _defaultKey1, Value = _currentValue1 });
            dbContext.Settings.Add(new Setting { Key = _defaultKey2, Value = _currentValue2 });
            dbContext.Settings.Add(new Setting { Key = _dbKey1, Value = _dbValue1 });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _configuration[_defaultKey1].Should().Be(_defaultValue1);

            SettingsModule.Instance.UseModule(_app, null!);

            _configuration[_defaultKey1].Should().Be(_currentValue1);
            _configuration[_defaultKey2].Should().Be(_currentValue2);
            _configuration[_defaultKey3].Should().Be(_defaultValue3);
            _configuration[_dbKey1].Should().Be(_dbValue1);
        }
        finally
        {
            await ((SettingsDbContext)dbContext).Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ConfigurationIsReloadedWhenSettingsAreSavedToDatabase()
    {
        using var dbContext = _app.Services.GetRequiredService<ISettingsDbContext>();

        await ((SettingsDbContext)dbContext).Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        try
        {
            SettingsModule.Instance.UseModule(_app, null!);

            _configuration[_defaultKey1].Should().Be(_defaultValue1);

            dbContext.Settings.Add(new Setting { Key = _defaultKey1, Value = _currentValue1 });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _configuration[_defaultKey1].Should().Be(_currentValue1);
        }
        finally
        {
            await ((SettingsDbContext)dbContext).Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        }
    }
}
