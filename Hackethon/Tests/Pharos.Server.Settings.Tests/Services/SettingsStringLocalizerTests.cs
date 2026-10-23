using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Localization;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;


namespace Pharos.Server.Settings.Services;

public class SettingsStringLocalizerTests
{
    [Fact]
    public void Indexer_WhenTranslationExistsInSettingsResource_ShouldResolveMergedFallbackResource()
    {
        // Arrange
        var sut = CreateLocalizer();

        // Act
        var result = sut["Settings_General_Group_Header"];

        // Assert
        result.ResourceNotFound.Should().BeFalse();
        result.Value.Should().Be(Translations.Settings_General_Group_Header);
    }

    [Fact]
    public void GetAllStrings_ShouldReturnEntriesFromBothMergedResourceSets()
    {
        // Arrange
        var sut = CreateLocalizer();

        // Act
        var allTranslations = sut.GetAllStrings(includeParentCultures: true)
            .ToDictionary(localizedString => localizedString.Name, localizedString => localizedString.Value, StringComparer.Ordinal);

        // Assert
        allTranslations.Should().ContainKey("Settings_General_Group_Header");
        allTranslations["Settings_General_Group_Header"].Should().Be(Translations.Settings_General_Group_Header);
        allTranslations.Should().ContainKey("AstmCommunicationSettings_CommunicationMode");
        allTranslations["AstmCommunicationSettings_CommunicationMode"].Should().Be(Translations.AstmCommunicationSettings_CommunicationMode);
    }

    [Fact]
    public void TranslationSearchProvider_WhenSearchingMergedResources_ShouldIncludeSettingsModuleTranslations()
    {
        // Arrange
        var localizer = CreateLocalizer();
        var sut = new SettingsTranslationSearchProvider(localizer);

        // Act
        var result = sut.GetMatchingTranslations(Translations.Settings_General_Group_Header);

        // Assert
        result.Should().Contain(match =>
            match.Key == nameof(Translations.Settings_General_Group_Header) &&
            match.Value == Translations.Settings_General_Group_Header);
    }

    private static ISettingsStringLocalizer CreateLocalizer()
    {
        EntityConfigurationProvider.StringLocalizerResourceType = typeof(Translations);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddTransient<ISettingsStringLocalizer, SettingsStringLocalizer>();

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ISettingsStringLocalizer>();
    }
}

