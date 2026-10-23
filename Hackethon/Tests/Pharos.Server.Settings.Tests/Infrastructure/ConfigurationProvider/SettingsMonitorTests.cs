using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Infrastructure.Settings;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class SettingsMonitorTests
{
    private readonly WebApplication _app;
    private readonly List<object> _receivedEvents = [];
    private readonly IConfigurationProvider _configurationProvider;
    private readonly SettingsMonitor _sut;

    public SettingsMonitorTests()
    {
        EntityConfigurationProvider.Instance = null;

        var appBuilder = WebApplication.CreateBuilder();
        var configBuilder = (IConfigurationBuilder)appBuilder.Configuration;

        // Use real DefaultSettingsProvider through EntityConfigurationProvider
        var defaultSettings = DefaultSettingsProvider.GetDefaultSettings(
            new[] { GetType().Assembly },
            _ => new[] { typeof(MonitoredRootSettings), typeof(MonitoredChildSettings), typeof(UnmonitoredSettings) });

        EntityConfigurationProvider.Instance = null;
        _configurationProvider = EntityConfigurationProvider.Create(defaultSettings);
        var configSourceMock = new Mock<IConfigurationSource>();
        configSourceMock
            .Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
            .Returns(() => _configurationProvider);

        configBuilder.Add(configSourceMock.Object);

        appBuilder.Services.AddMediatR(config =>
        {
            config.Lifetime = ServiceLifetime.Scoped;
            config.RegisterServicesFromAssemblies([GetType().Assembly]);
        });

        // Configure settings
        appBuilder.Services.ConfigureSettings<MonitoredRootSettings>(appBuilder.Configuration);
        appBuilder.Services.ConfigureSettings<MonitoredChildSettings>(appBuilder.Configuration);
        appBuilder.Services.ConfigureSettings<UnmonitoredSettings>(appBuilder.Configuration);

        // Register notification handlers
        appBuilder.Services.AddScoped<INotificationHandler<SettingsChangedEvent<MonitoredRootSettings>>, MonitoredRootSectionHandler>();
        appBuilder.Services.AddScoped<INotificationHandler<SettingsChangedEvent<MonitoredChildSettings>>, MonitoredChildSectionHandler>();
        // Note: No handler for UnmonitoredSection

        // Register test background event publisher
        appBuilder.Services.AddScoped<IPublisher>(_ => new TestBackgroundEventPublisher(_receivedEvents));

        appBuilder.Configuration["ConnectionStrings:" + DatabaseSettings.MainConnectionStringName] = null;
        SettingsModule.Instance.AddServices(appBuilder.Services, appBuilder.Configuration, null);

        _app = appBuilder.Build();
        _sut = new SettingsMonitor(_configurationProvider, appBuilder.Services, _app.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<SettingsMonitor>.Instance);
    }

    [Fact]
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public async Task SettingsMonitor_DiscoversMonitoredSettings_WhenHandlersAreRegistered()
    {
        // Act
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        // The monitor should have discovered handlers for MonitoredRootSection and MonitoredChildSection
        // but not for UnmonitoredSection (no handler registered)
        var monitoredSettings = _sut.MonitoredSettings;
        monitoredSettings.Should().Contain(typeof(MonitoredRootSettings))
        .And.Contain(typeof(MonitoredChildSettings))
        .And.NotContain(typeof(UnmonitoredSettings));

        await _sut.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SettingsMonitor_PublishesEvent_WhenRootConfigurationChanges()
    {
        // Arrange
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        // Act - change configuration by updating the provider's data
        UpdateProviderData("MonitoredRoot:IntValue", "999");
        UpdateProviderData("MonitoredRoot:StringValue", "Changed");
        TriggerConfigurationReload();

        // Wait a bit for async processing
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert
        _receivedEvents.Should().HaveCount(1);
        var settingsChangedEvent = _receivedEvents.First().Should().BeOfType<SettingsChangedEvent<MonitoredRootSettings>>().Subject;

        settingsChangedEvent.OldValue.IntValue.Should().Be(10);
        settingsChangedEvent.OldValue.StringValue.Should().Be("DefaultRoot");

        settingsChangedEvent.NewValue.IntValue.Should().Be(999);
        settingsChangedEvent.NewValue.StringValue.Should().Be("Changed");

        await _sut.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SettingsMonitor_PublishesEvent_WhenChildConfigurationChanges()
    {
        // Arrange
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        // Act - change child configuration
        UpdateProviderData("MonitoredChild:BoolValue", "false");
        UpdateProviderData("MonitoredChild:DoubleValue", "99.99");
        TriggerConfigurationReload();

        // Wait a bit for async processing
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert
        _receivedEvents.Should().HaveCount(1);
        var settingsChangedEvent = _receivedEvents.First().Should().BeOfType<SettingsChangedEvent<MonitoredChildSettings>>().Subject;

        settingsChangedEvent.OldValue.BoolValue.Should().Be(true);
        settingsChangedEvent.OldValue.DoubleValue.Should().Be(42.5);

        settingsChangedEvent.NewValue.BoolValue.Should().Be(false);
        settingsChangedEvent.NewValue.DoubleValue.Should().Be(99.99);

        await _sut.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SettingsMonitor_DoesNotPublishEvent_WhenUnmonitoredConfigurationChanges()
    {
        // Arrange
        await _sut.StartAsync(TestContext.Current.CancellationToken);

        // Act - change unmonitored configuration
        UpdateProviderData("Unmonitored:Value", "NewValue");
        TriggerConfigurationReload();

        // Wait a bit for async processing
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert
        _receivedEvents.Should().BeEmpty();

        await _sut.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SettingsMonitor_DoesNotPublishEvent_WhenConfigurationDoesNotChange()
    {
        // Arrange
        await _sut.StartAsync(TestContext.Current.CancellationToken);

        // Act - trigger reload without actually changing values
        TriggerConfigurationReload();

        // Wait a bit for async processing
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert
        _receivedEvents.Should().BeEmpty();

        await _sut.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SettingsMonitor_PublishesMultipleEvents_WhenMultipleSectionsChange()
    {
        // Arrange
        await _sut.StartAsync(TestContext.Current.CancellationToken);
        await _sut.ExecuteTask!.WaitAsync(TestContext.Current.CancellationToken);

        // Act - change both monitored configurations
        UpdateProviderData("MonitoredRoot:IntValue", "888");
        UpdateProviderData("MonitoredChild:BoolValue", "false");
        TriggerConfigurationReload();

        // Wait a bit for async processing
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Assert
        _receivedEvents.Should().HaveCount(2);

        var rootEvent = _receivedEvents.OfType<SettingsChangedEvent<MonitoredRootSettings>>().Should().ContainSingle().Subject;
        rootEvent.NewValue.IntValue.Should().Be(888);

        var childEvent = _receivedEvents.OfType<SettingsChangedEvent<MonitoredChildSettings>>().Should().ContainSingle().Subject;
        childEvent.NewValue.BoolValue.Should().Be(false);

        await _sut.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public void SettingsMonitor_DefaultValuesFromDefaultSettingsProvider_AreCorrectlyLoaded()
    {
        // Arrange & Act
        using var scope = _app.Services.CreateAsyncScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Assert - Verify that DefaultSettingsProvider correctly generated the default values
        configuration["MonitoredRoot:IntValue"].Should().Be("10");
        configuration["MonitoredRoot:StringValue"].Should().Be("DefaultRoot");
        configuration["MonitoredChild:BoolValue"].Should().Be("True");
        configuration["MonitoredChild:DoubleValue"].Should().Be("42.5");
        configuration["Unmonitored:Value"].Should().Be("Default");
    }

    private void UpdateProviderData(string key, string? value)
    {
        // Access the internal Data dictionary of EntityConfigurationProvider
        var dataProperty = typeof(EntityConfigurationProvider).BaseType!.GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var data = (IDictionary<string, string?>)dataProperty!.GetValue(_configurationProvider)!;
        data[key] = value;
    }

    private void TriggerConfigurationReload()
    {
        // Use reflection to call the protected OnReload method
        var onReloadMethod = typeof(EntityConfigurationProvider).BaseType!.GetMethod("OnReload", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onReloadMethod!.Invoke(_configurationProvider, null);
    }

    #region Test Configuration Sections

    [RootConfigSection(sortOrder: 1)]
    private class MonitoredRootSettings
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public int IntValue { get; set; } = 10;

        public string StringValue { get; set; } = "DefaultRoot";
    }

    private class MonitoredChildSettings
    {
        public bool BoolValue { get; set; } = true;

        public double DoubleValue { get; set; } = 42.5;
    }

    [RootConfigSection(sortOrder: 2)]
    private class UnmonitoredSettings
    {
        // ReSharper disable once UnusedMember.Local
        public string Value { get; set; } = "Default";
    }

    #endregion

    #region Test Notification Handlers

    private class MonitoredRootSectionHandler : INotificationHandler<SettingsChangedEvent<MonitoredRootSettings>>
    {
        public Task Handle(SettingsChangedEvent<MonitoredRootSettings> notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private class MonitoredChildSectionHandler : INotificationHandler<SettingsChangedEvent<MonitoredChildSettings>>
    {
        public Task Handle(SettingsChangedEvent<MonitoredChildSettings> notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    #endregion

    private class TestBackgroundEventPublisher(List<object> eventCapture) : IPublisher
    {
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            eventCapture.Add(notification);
            return Task.CompletedTask;
        }
    }
}