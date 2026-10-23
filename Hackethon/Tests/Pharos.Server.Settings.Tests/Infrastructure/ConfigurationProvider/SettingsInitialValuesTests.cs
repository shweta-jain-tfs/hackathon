using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class SettingsInitialValuesTests
{
    private static IDictionary<string, string?> GetSettings(Type configSectionType, bool? isDevelopment = null)
        => DefaultSettingsProvider.GetDefaultSettings(
            [typeof(SettingsInitialValuesTests).Assembly],
            _ => [configSectionType],
            isDevelopment: isDevelopment);

    #region Initial values

    [Theory]
    [InlineData(null, "default")]
    [InlineData(true, "dev")]
    [InlineData(false, "prod")]
    public void InitialScalarValue_IsAppliedBasedOnIsDevelopment(bool? isDevelopment, string expectedValue)
    {
        var settings = GetSettings(typeof(InitializableValueSection), isDevelopment: isDevelopment);

        settings[nameof(InitializableValueSection) + ":Value"].Should().Be(expectedValue);
    }

    [Fact]
    public void InitialList_IsNotInitialized_WhenIsDevelopmentIsNull()
    {
        var settings = GetSettings(typeof(InitializableListSection), isDevelopment: null);

        settings.Keys.Should().NotContain(k => k.StartsWith(nameof(InitializableListSection) + ":Items:"));
    }

    [Theory]
    [InlineData(true, "dev-item")]
    [InlineData(false, "prod-item")]
    public void InitialList_IsInitialized_WhenIsDevelopmentIsSet(bool isDevelopment, string expectedName)
    {
        var settings = GetSettings(typeof(InitializableListSection), isDevelopment: isDevelopment);

        settings[nameof(InitializableListSection) + ":Items:0:Name"].Should().Be(expectedName);
    }

    #endregion Initial values

    #region Unique constraints

    [Fact]
    public void NoUniqueProperties_WithDuplicateValues_DoesNotThrow()
    {
        var act = () => GetSettings(typeof(NoUniquePropertiesSection), isDevelopment: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void UniqueStringProperty_WithDistinctValues_DoesNotThrow()
    {
        var act = () => GetSettings(typeof(UniqueStringDistinctSection), isDevelopment: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void UniqueStringProperty_WithDuplicateValues_ThrowsValidationException()
    {
        var act = () => GetSettings(typeof(UniqueStringDuplicateSection), isDevelopment: true);

        act.Should().Throw<ValidationException>()
            .WithMessage("*Items*Name*alpha*");
    }

    [Fact]
    public void CaseInsensitiveUniqueStringProperty_WithMixedCaseDuplicates_ThrowsValidationException()
    {
        var act = () => GetSettings(typeof(CaseInsensitiveDuplicateSection), isDevelopment: true);

        act.Should().Throw<ValidationException>()
            .WithMessage("*Items*Name*");
    }

    [Fact]
    public void CaseSensitiveUniqueStringProperty_WithDifferentCaseValues_DoesNotThrow()
    {
        var act = () => GetSettings(typeof(CaseSensitiveDifferentCaseSection), isDevelopment: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void UniqueIntegerProperty_WithDuplicateValues_ThrowsValidationException()
    {
        var act = () => GetSettings(typeof(UniqueIntDuplicateSection), isDevelopment: true);

        act.Should().Throw<ValidationException>()
            .WithMessage("*Items*Id*1*");
    }

    [Fact]
    public void UniqueStringProperty_WithMultipleNullValues_ThrowsValidationException()
    {
        var act = () => GetSettings(typeof(NullValueDuplicateSection), isDevelopment: true);

        act.Should().Throw<ValidationException>()
            .WithMessage("*Items*Name*null*");
    }

    [Fact]
    public void UniqueConstraints_AreNotValidated_WhenIsDevelopmentIsNull()
    {
        // ValidateUniqueConstraints is only called when initializedSection = true,
        // which requires IsDevelopment != null. Pre-populated duplicates are ignored.
        var act = () => GetSettings(typeof(PrePopulatedDuplicateSection), isDevelopment: null);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UniqueConstraints_AreValidated_WhenIsDevelopmentIsSet(bool isDevelopment)
    {
        var act = () => GetSettings(typeof(PrePopulatedDuplicateSection), isDevelopment: isDevelopment);

        act.Should().Throw<ValidationException>();
    }

    #endregion Unique constraints

    #region Section and item classes

    [RootConfigSection(sortOrder: 100)]
    private class NoUniquePropertiesSection : IConfigSectionWithInitialValues
    {
        public List<PlainItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Name = "alpha" }, new() { Name = "alpha" }];
    }

    [RootConfigSection(sortOrder: 101)]
    private class UniqueStringDistinctSection : IConfigSectionWithInitialValues
    {
        public List<UniqueStringItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Name = "alpha" }, new() { Name = "beta" }];
    }

    [RootConfigSection(sortOrder: 102)]
    private class UniqueStringDuplicateSection : IConfigSectionWithInitialValues
    {
        public List<UniqueStringItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Name = "alpha" }, new() { Name = "alpha" }];
    }

    [RootConfigSection(sortOrder: 103)]
    private class CaseInsensitiveDuplicateSection : IConfigSectionWithInitialValues
    {
        public List<CaseInsensitiveUniqueItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Name = "alpha" }, new() { Name = "ALPHA" }];
    }

    [RootConfigSection(sortOrder: 104)]
    private class CaseSensitiveDifferentCaseSection : IConfigSectionWithInitialValues
    {
        public List<UniqueStringItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Name = "alpha" }, new() { Name = "ALPHA" }];
    }

    [RootConfigSection(sortOrder: 105)]
    private class UniqueIntDuplicateSection : IConfigSectionWithInitialValues
    {
        public List<UniqueIntItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Id = 1 }, new() { Id = 1 }];
    }

    [RootConfigSection(sortOrder: 106)]
    private class NullValueDuplicateSection : IConfigSectionWithInitialValues
    {
        public List<UniqueStringItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = [new() { Name = null }, new() { Name = null }];
    }

    [RootConfigSection(sortOrder: 107)]
    private class PrePopulatedDuplicateSection : IConfigSectionWithInitialValues
    {
        public List<UniqueStringItem> Items { get; set; } = [new() { Name = "alpha" }, new() { Name = "alpha" }];
        public void InitializeConfigSection(bool isDevelopment) { }
    }

    [RootConfigSection(sortOrder: 108)]
    private class InitializableValueSection : IConfigSectionWithInitialValues
    {
        public string Value { get; set; } = "default";
        public void InitializeConfigSection(bool isDevelopment)
            => Value = isDevelopment ? "dev" : "prod";
    }

    [RootConfigSection(sortOrder: 109)]
    private class InitializableListSection : IConfigSectionWithInitialValues
    {
        public List<PlainItem> Items { get; set; } = [];
        public void InitializeConfigSection(bool isDevelopment)
            => Items = isDevelopment ? [new() { Name = "dev-item" }] : [new() { Name = "prod-item" }];
    }

    private class PlainItem
    {
        public string? Name { get; set; }
    }

    private class UniqueStringItem
    {
        [Unique]
        public string? Name { get; set; }
    }

    private class CaseInsensitiveUniqueItem
    {
        [Unique(isCaseInsensitive: true)]
        public string? Name { get; set; }
    }

    private class UniqueIntItem
    {
        [Unique]
        public int Id { get; set; }
    }

    #endregion Section and item classes
}
