using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pharos.Server.Domain.Common;
using Pharos.Server.Infrastructure.Logging;
using Serilog.Events;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Pharos.Server.Infrastructure.Tests.Logging;

[Collection("Serilog")]
public class CustomDestructuringPolicyTests : IAsyncDisposable
{
    private readonly WebApplication _application;
    private readonly ILogger _logger;
    private readonly CollectingLogEventSink _sink = new();

    public CustomDestructuringPolicyTests()
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

        logger.LogInformation("Logging payload {@Payload}", new RedactionPayload { Password = "password" });

        var payload = Assert.IsType<StructureValue>(sink.LogEvent!.Properties["Payload"]);
        var properties = payload.Properties.ToDictionary(property => property.Name, property => property.Value);
        Assert.Equal("password", Assert.IsType<ScalarValue>(properties["Password"]).Value);
    }

    private static WebApplication CreateApplication(CollectingLogEventSink sink, bool? redactionEnabled = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        var configuration = new Dictionary<string, string?>
        {
            ["Serilog:MinimumLevel:Default"] = "Information",
            [PropertyRedactionExtensions.RedactSensitiveDataKey] = (redactionEnabled ?? true).ToString()
        };

        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Host.ConfigureSerilog(configuration => configuration.WriteTo.Sink(sink));
        return builder.Build();
    }

    [Fact]
    public void ProtectedAndSensitivePropertiesAreRedacted()
    {
        _logger.LogInformation("Logging payload {@Payload}", new RedactionPayload
        {
            ProtectedData = "protected data",
            ProtectedPersonalData = "protected personal data",
            PersonalData = "personal data",
            User = "user1",
            NewPassword = "new_password",
            Password = "password",
            SafeValue = "safe value"
        });

        var payload = Assert.IsType<StructureValue>(_sink.LogEvent!.Properties["Payload"]);
        var properties = payload.Properties.ToDictionary(property => property.Name, property => property.Value);

        AssertRedacted(properties["ProtectedData"]);
        AssertRedacted(properties["ProtectedPersonalData"]);
        AssertRedacted(properties["PersonalData"]);
        AssertRedacted(properties["User"]);
        AssertRedacted(properties["NewPassword"]);
        AssertRedacted(properties["Password"]);
        Assert.Equal("safe value", Assert.IsType<ScalarValue>(properties["SafeValue"]).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptySensitivePropertiesAreNotRedacted(string propertyValue)
    {
        _logger.LogInformation("Logging payload {@Payload}", new RedactionPayload
        {
            Password = propertyValue,
            User = propertyValue
        });

        var payload = Assert.IsType<StructureValue>(_sink.LogEvent!.Properties["Payload"]);
        var properties = payload.Properties.ToDictionary(property => property.Name, property => property.Value);

        Assert.Equal(propertyValue, Assert.IsType<ScalarValue>(properties["Password"]).Value);
        Assert.Equal(propertyValue, Assert.IsType<ScalarValue>(properties["User"]).Value);
    }

    [Fact]
    public void ProtectedObjectPropertyDeclaredOnInterfaceIsRedacted()
    {
        _logger.LogInformation("Logging payload {@Payload}", new InterfaceProtectedPayload
        {
            EventData = new EventDataPayload { Value = "sensitive value" }
        });

        var payload = Assert.IsType<StructureValue>(_sink.LogEvent!.Properties["Payload"]);
        var properties = payload.Properties.ToDictionary(property => property.Name, property => property.Value);

        AssertRedacted(properties["EventData"]);
    }

    [Fact]
    public void ProtectedObjectPropertyDeclaredOnClassIsRedacted()
    {
        _logger.LogInformation("Logging payload {@Payload}", new ClassProtectedPayload
        {
            EventData = new EventDataPayload { Value = "sensitive value" }
        });

        var payload = Assert.IsType<StructureValue>(_sink.LogEvent!.Properties["Payload"]);
        var properties = payload.Properties.ToDictionary(property => property.Name, property => property.Value);

        AssertRedacted(properties["EventData"]);
    }

    public ValueTask DisposeAsync()
    {
        return _application.DisposeAsync();
    }

    private static void AssertRedacted(LogEventPropertyValue propertyValue)
    {
        Assert.Equal(PropertyRedactionExtensions.RedactedValue, Assert.IsType<ScalarValue>(propertyValue).Value);
    }

    private sealed class RedactionPayload
    {
        [ProtectedData]
        public string? ProtectedData { get; init; }

        [ProtectedPersonalData]
        public string? ProtectedPersonalData { get; init; }

        [PersonalData]
        public string? PersonalData { get; init; }

        public string? User { get; init; }

        public string? Password { get; init; }

        public string? NewPassword { get; init; }

        public string? SafeValue { get; init; }
    }

    private interface IInterfaceProtectedPayload
    {
        [ProtectedData]
        EventDataPayload EventData { get; }
    }

    private sealed class InterfaceProtectedPayload : IInterfaceProtectedPayload
    {
        public EventDataPayload EventData { get; init; } = null!;
    }

    private sealed class ClassProtectedPayload
    {
        [ProtectedData]
        public EventDataPayload EventData { get; init; } = null!;
    }

    private sealed class EventDataPayload
    {
        public string? Value { get; init; }
    }

}
