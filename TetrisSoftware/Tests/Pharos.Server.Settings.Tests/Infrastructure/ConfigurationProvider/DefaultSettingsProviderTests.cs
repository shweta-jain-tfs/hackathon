using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Infrastructure.Settings;
using ConfigurationManager = Microsoft.Extensions.Configuration.ConfigurationManager;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class DefaultSettingsProviderTests
{
    [Theory]
    [InlineData(typeof(AbstractConfigSection))]
    [InlineData(typeof(NoDefaultConstructorConfigSection))]
    [InlineData(typeof(RecursiveConfigSection))]
    [InlineData(typeof(DictionaryConfigSection), typeof(NotSupportedException))]
    [InlineData(typeof(DateTimeConfigSection), typeof(NotSupportedException))]
    public void InvalidConfigSections(Type configSectionType, Type? exceptionType = null)
    {
        exceptionType ??= typeof(InvalidOperationException);
        try
        {
            using var configuration = new ConfigurationManager();
            DefaultSettingsProvider.GetDefaultSettings(new[] { GetType().Assembly }, _ => new[] { configSectionType });
            Assert.Fail();
        }
        catch (Exception e)
        {
            e.Should().BeOfType(exceptionType);
        }
    }

    [Theory]
    [InlineData("IntZero", "0")]
    [InlineData("IntNonZero", "2")]
    [InlineData("IntNull", null)]
    [InlineData("IntNullable", "3")]
    [InlineData("IntDefault", "4")]
    [InlineData("BoolFalse", "False")]
    [InlineData("BoolTrue", "True")]
    [InlineData("BoolNull", null)]
    [InlineData("BoolDefault", "False")]
    [InlineData("Double", "-12.34")]
    [InlineData("DoubleNull", "56.78")]
    [InlineData("String", "DefStr")]
    [InlineData("DateTime", "0001-01-01")]
    [InlineData("DateTimeDef", "2024-06-12")]
    [InlineData("Enum", "EnumValue2")]
    public void ValuePropertyTests(string propertyName, string? value)
    {
        var settings = DefaultSettingsProvider.GetDefaultSettings(
            new[] { GetType().Assembly },
            _ => new[] { typeof(ValueConfigSection) });

        var key = nameof(ValueConfigSection) + ":" + propertyName;
        settings.Should().ContainKey(key);
        settings[key].Should().Be(value);
    }

    [Fact]
    public void ActionButtonProperties_AreNotDefaultSettings()
    {
        var settings = DefaultSettingsProvider.GetDefaultSettings(
            new[] { GetType().Assembly },
            _ => new[] { typeof(ActionButtonConfigSection) });

        settings.Should().ContainKey(nameof(ActionButtonConfigSection) + ":" + nameof(ActionButtonConfigSection.RegularBoolean));
        settings.Should().NotContainKey(nameof(ActionButtonConfigSection) + ":" + nameof(ActionButtonConfigSection.ExportButton));
    }

    [Fact]
    public void ArrayTest()
    {
        var settings = DefaultSettingsProvider.GetDefaultSettings(
            new[] { GetType().Assembly },
            _ => new[] { typeof(ArrayConfigSection) });

        settings.Should().HaveCount(3);
        var keyPrefix1 = nameof(ArrayConfigSection) + ":" + nameof(ArrayConfigSection.Array);
        settings[keyPrefix1 + ":0"].Should().Be(null);
        settings[keyPrefix1 + ":1" + ":" + nameof(NestedObject.Int)].Should().Be("0");

        var keyPrefix2 = nameof(ArrayConfigSection) + ":" + nameof(ArrayConfigSection.List);
        settings[keyPrefix2 + ":0" + ":" + nameof(NestedObject.Int)].Should().Be("0");
    }

    [Fact]
    public void NestedObjectTest()
    {
        var settings = DefaultSettingsProvider.GetDefaultSettings(
            new[] { GetType().Assembly },
            _ => new[] { typeof(NestedObjectConfigSection) });

        settings.Should().HaveCount(1);
        settings[nameof(NestedObjectConfigSection) + ":" + nameof(NestedObjectConfigSection.NestedObjectProp) + ":" + nameof(NestedObject.Int)].Should().Be("0");
    }

    [Fact]
    public void SuffixesAreRemovedFromClassName()
    {
        var settings = DefaultSettingsProvider.GetDefaultSettings(
            new[] { GetType().Assembly },
            _ => new[] { typeof(TestOptions), typeof(Options), typeof(Test1Settings), typeof(Settings) });

        settings.Should().HaveCount(4);
        settings.Should().ContainKey("Test:Int");
        settings.Should().ContainKey("Options:Int");
        settings.Should().ContainKey("Test1:Int");
        settings.Should().ContainKey("Settings:Int");
    }

    [Fact]
    public void ConfigureSettings()
    {
        var appBuilder = WebApplication.CreateBuilder();
        var configBuilder = (IConfigurationBuilder)appBuilder.Configuration;

        EntityConfigurationProvider.Instance = null;
        var configSourceMock = new Mock<IConfigurationSource>();
        configSourceMock
            .Setup(s => s.Build(It.IsAny<IConfigurationBuilder>()))
            .Returns(() =>
            {
                var defaultSettings = DefaultSettingsProvider.GetDefaultSettings(
                    new[] { GetType().Assembly },
                    _ => new[] { typeof(RootConfigSection), typeof(ChildConfigSection) });
                return EntityConfigurationProvider.Create(defaultSettings);
            });

        configBuilder.Add(configSourceMock.Object);

        appBuilder.Services.ConfigureSettings<RootConfigSection>(appBuilder.Configuration);
        appBuilder.Services.ConfigureSettings<ChildConfigSection>(appBuilder.Configuration);

        appBuilder.Configuration["ConnectionStrings:" + DatabaseSettings.MainConnectionStringName] = null; // to use in-memory database for tests
        SettingsModule.Instance.AddServices(appBuilder.Services, appBuilder.Configuration, null);
        var app = appBuilder.Build();
        var configuration = app.Configuration;

        var rootOptions = app.Services.GetRequiredService<IOptions<RootConfigSection>>();
        rootOptions.Value.Int.Should().Be(1);
        rootOptions.Value.Child.Should().NotBeNull();
        rootOptions.Value.Child!.Prop.Should().Be(2);

        var childOptions = app.Services.GetRequiredService<IOptions<ChildConfigSection>>();
        childOptions.Value.Prop.Should().Be(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(nameof(RootConfigSection))]
    [InlineData("NotFound")]
    public void ConfigureSettingsWithError(string? key)
    {
        using var configuration = new ConfigurationManager();
        if (key == null)
        {
            configuration[nameof(ChildConfigSection)] = null;
        }
        else
        {
            configuration[key + ":" + key] = "DoesNotMatter";
        }

        var services = new ServiceCollection();
        try
        {
            services.ConfigureSettings<ChildConfigSection>(configuration);
            Assert.Fail();
        }
        catch (InvalidOperationException)
        {
        }
    }

    [Fact]
    public void ConfigureSettingsForRootSectionAlwaysWorks()
    {
        using var configuration = new ConfigurationManager();
        var services = new ServiceCollection();
        services.ConfigureSettings<RootConfigSection>(configuration);
    }

    #region InvalidConfigSections

    [RootConfigSection(sortOrder: 1)]
    private abstract class AbstractConfigSection { }

    [RootConfigSection(sortOrder: 2)]
#pragma warning disable CS9113 // Parameter is unread.
    private class NoDefaultConstructorConfigSection(int param) { }
#pragma warning restore CS9113 // Parameter is unread.

    [RootConfigSection(sortOrder: 3)]
    private class DictionaryConfigSection
    {
        public IDictionary<string, object>? Property { get; set; }
    }

    [RootConfigSection(sortOrder: 4)]
    private class DateTimeConfigSection
    {
        public DateTime Property { get; set; }
    }

    [RootConfigSection(sortOrder: 5)]
    private class RecursiveConfigSection
    {
        public RecursiveClass? Prop { get; set; }
    }

    private class RecursiveClass
    {
        public RecursiveConfigSection? Prop { get; set; }
    }

    #endregion InvalidConfigSections

    #region ValidConfigSections

    [RootConfigSection(sortOrder: 6)]
    private class ValueConfigSection
    {
        public int IntZero { get; set; }
        public int IntNonZero { get; set; } = 2;
        public int? IntNull { get; set; }
        public int? IntNullable { get; set; } = 3;
        public int IntDefault { get; set; } = 4;
        public bool BoolFalse { get; set; }
        public bool BoolTrue { get; set; } = true;
        public bool? BoolNull { get; set; }
        public bool? BoolDefault { get; set; } = false;
        public double Double { get; set; } = -12.34;
        public double? DoubleNull { get; set; } = 56.78;
        public string? String { get; set; } = "DefStr";
        public DateTimeOffset DateTime { get; set; }
        public DateTimeOffset? DateTimeDef { get; set; } = new DateTimeOffset(2024, 6, 12, 0, 0, 0, TimeSpan.Zero);
        public TestEnum Enum { get; set; } = TestEnum.EnumValue2;
    }

    [RootConfigSection(sortOrder: 14)]
    private class ActionButtonConfigSection
    {
        public bool RegularBoolean { get; set; } = true;

        public SettingsAction ExportButton { get; set; } = new(ActionKey.Export);
    }

    [RootConfigSection(sortOrder: 7)]
    private class ArrayConfigSection
    {
        public IEnumerable<NestedObject?> Array { get; set; } = new[] { null, new NestedObject() };
        public List<NestedObject> List { get; set; } = new() { new NestedObject() };
    }

    [RootConfigSection(sortOrder: 8)]
    private class NestedObjectConfigSection
    {
        public NestedObject NestedObjectProp { get; set; } = new();
    }

    private class NestedObject
    {
        public int Int { get; set; }
    }

    [RootConfigSection(sortOrder: 9)]
    private class Options
    {
        public int Int { get; set; }
    }

    [RootConfigSection(sortOrder: 10)]
    private class TestOptions
    {
        public int Int { get; set; }
    }

    [RootConfigSection(sortOrder: 11)]
    private class Settings
    {
        public int Int { get; set; }
    }

    [RootConfigSection(sortOrder: 12)]
    private class Test1Settings
    {
        public int Int { get; set; }
    }

    [RootConfigSection(sortOrder: 13)]
    private class RootConfigSection
    {
        public int Int { get; set; } = 1;

        public ChildConfigSection? Child { get; set; }
    }

    private class ChildConfigSection
    {
        public int Prop { get; set; } = 2;
    }

    private enum TestEnum
    {
        EnumValue1,
        EnumValue2
    }

    #endregion ValidConfigSections
}
