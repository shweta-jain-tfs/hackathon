using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Pharos.Server.Infrastructure.Tests.Logging;

[Collection("Serilog")]
public class SensitivePropertyRedactionEnricherTests : IAsyncDisposable
{
    private readonly WebApplication _application;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly CollectingLogEventSink _sink = new();

    public SensitivePropertyRedactionEnricherTests()
    {
        _application = CreateApplication(_sink);
        _logger = _application.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Test");
    }

    [Fact]
    public async Task SensitivePropertiesAreNotRedactedWhenDisabled()
    {
        var sink = new CollectingLogEventSink();
        await using var application = CreateApplication(sink, redactionEnabled: false);
        var logger = application.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Test");

        using (logger.BeginScope(new Dictionary<string, object?> { ["Token"] = "sensitive value" }))
        {
            logger.LogInformation("Logging property");
        }

        Assert.Equal("sensitive value", Assert.IsType<ScalarValue>(sink.LogEvent!.Properties["Token"]).Value);
    }

    private static WebApplication CreateApplication(CollectingLogEventSink sink, bool? redactionEnabled = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        var configuration = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:Enrich:0"] = "FromLogContext",
            [PropertyRedactionExtensions.RedactSensitiveDataKey] = (redactionEnabled ?? true).ToString()
        };

        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Host.ConfigureSerilog(configuration => configuration.WriteTo.Sink(sink));
        return builder.Build();
    }

    [Theory]
    [InlineData("Token")]
    [InlineData("PASSWORD")]
    [InlineData("NewPassword")]
    public void SensitivePropertiesAreRedacted(string propertyName)
    {
        using (_logger.BeginScope(new Dictionary<string, object?> { [propertyName] = "sensitive value" }))
        {
            _logger.LogInformation("Logging property");
        }

        Assert.Equal(PropertyRedactionExtensions.RedactedValue, Assert.IsType<ScalarValue>(_sink.LogEvent!.Properties[propertyName]).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptySensitivePropertiesAreNotRedacted(string propertyValue)
    {
        using (_logger.BeginScope(new Dictionary<string, object?> { ["Token"] = propertyValue }))
        {
            _logger.LogInformation("Logging property");
        }

        Assert.Equal(propertyValue, Assert.IsType<ScalarValue>(_sink.LogEvent!.Properties["Token"]).Value);
    }

    [Fact]
    public void NonSensitivePropertiesAreNotChanged()
    {
        using (_logger.BeginScope(new Dictionary<string, object?> { ["SafeValue"] = "safe value" }))
        {
            _logger.LogInformation("Logging property");
        }

        Assert.Equal("safe value", Assert.IsType<ScalarValue>(_sink.LogEvent!.Properties["SafeValue"]).Value);
    }

    public ValueTask DisposeAsync()
    {
        return _application.DisposeAsync();
    }
}

public sealed class CollectingLogEventSink : ILogEventSink
{
    public LogEvent? LogEvent { get; private set; }

    public void Clear()
    {
        LogEvent = null;
    }

    public void Emit(LogEvent logEvent)
    {
        LogEvent = logEvent;
    }
}

[CollectionDefinition("Serilog", DisableParallelization = true)]
public sealed class SerilogCollection;
