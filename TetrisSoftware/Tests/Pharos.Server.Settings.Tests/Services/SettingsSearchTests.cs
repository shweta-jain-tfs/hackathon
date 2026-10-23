using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Pharos.Server.Application.Queries;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Services;

public class SettingsSearchTests : IDisposable
{
    private readonly Mock<ISettingsDbContext> _dbContextMock;
    private readonly Mock<ISettingsStringLocalizer> _localizerMock;
    private readonly Mock<ISettingsPermissionHelper> _permissionHelperMock;
    private readonly Mock<ITranslationSearchProvider> _translationSearchProviderMock;
    private readonly Mock<ISettingsPagesLoader> _settingsPagesLoaderMock;
    private readonly SettingsSearch _sut;

    public SettingsSearchTests()
    {
        _dbContextMock = new Mock<ISettingsDbContext>();
        _localizerMock = new Mock<ISettingsStringLocalizer>();
        _permissionHelperMock = new Mock<ISettingsPermissionHelper>();
        _translationSearchProviderMock = new Mock<ITranslationSearchProvider>();
        _settingsPagesLoaderMock = new Mock<ISettingsPagesLoader>();

        // Setup localizer to return the key as the localized value (pass-through)
        SetupLocalizer();
        SetupDefaultSettingsPagesLoader();

        _sut = new SettingsSearch(
            _localizerMock.Object,
            _permissionHelperMock.Object,
            _translationSearchProviderMock.Object,
            _settingsPagesLoaderMock.Object);
    }

    public void Dispose()
    {
        BaseSettingsPagesProvider.Initialize([]);
    }

    private void SetupLocalizer()
    {
        _localizerMock.Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key =>
            {
                return new LocalizedString(key, key, true);
            });
    }

    private void SetupPermissionHelper(string grantedPermission)
    {
        _permissionHelperMock.Setup(x => x.HasPermissionAsync(
            It.Is<SettingItemBase>(item => item.Permissions.IsNullOrEmpty() || item.Permissions.Contains(grantedPermission)),
            null))
        .ReturnsAsync(true);

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(
            It.Is<SettingItemBase>(item => !item.Permissions.IsNullOrEmpty() && !item.Permissions.Contains(grantedPermission)),
            null))
        .ReturnsAsync(false);
    }

    private static List<TranslationSearchMatch> CreateMatches(params string[] resourceKeys)
    {
        return resourceKeys
            .Select(resourceKey => new TranslationSearchMatch(resourceKey, resourceKey))
            .ToList();
    }

    private void SetupDefaultSettingsPagesLoader()
    {
        _settingsPagesLoaderMock
            .Setup(x => x.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((rootPageLabel, _) => Task.FromResult(LoadPagesForSearch(rootPageLabel)));
    }

    private Dictionary<string, Page> LoadPagesForSearch(string rootPageLabel)
    {
        var settings = _dbContextMock.Object.Settings?.ToDictionary(setting => setting.Key, setting => setting.Value)
            ?? new Dictionary<string, string?>();

        return LoadPagesForSearch(rootPageLabel, settings);
    }

    private Dictionary<string, Page> LoadPagesForSearch(string rootPageLabel, Dictionary<string, string?> settings)
    {
        var rootPage = SettingsSearchPagesProvider.GetTopLevelPages(_localizerMock.Object)
            .Values
            .FirstOrDefault(page => page.Key == rootPageLabel);
        if (rootPage == null)
        {
            return [];
        }

        var populatedPage = SearchPopulator.PopulateTopLevelPage(rootPage, settings);
        return new Dictionary<string, Page>
        {
            [populatedPage.Key] = populatedPage
        };
    }

    [Fact]
    public async Task Search_WhenSearchStringIsNull_ShouldReturnEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(null!, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WhenSearchStringIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(string.Empty, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WhenSearchStringIsWhitespace_ShouldReturnEmptyList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search("   ", cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5399")]
    public async Task Search_WhenOptionLabelMatches_ShouldReturnOptionSearchResult()
    {
        // Arrange

        // Initialize SettingsSearchPagesProvider with the test assembly containing test configuration classes
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
            typeof(PermissionTestSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:Option1", Value = "value1" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
            .Setup(x => x.GetMatchingTranslations(searchString))
            .Returns(CreateMatches("TestOption"));

        // Setup permission to allow all
        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle(r => r.Match == "TestOption");
        result.Should().AllSatisfy(r => r.Module.Should().Be(SearchResultModules.Settings));
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5404")]
    public async Task Search_WhenUserHasPermission_ShouldIncludeResultInOutput()
    {
        // Arrange

        // Initialize SettingsSearchPagesProvider with the test assembly containing test configuration classes
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
            typeof(PermissionTestSettings),
        });

        var searchString = "Secure";
        var settings = new List<Setting>
        {
            new() { Key = "PermissionTestSettings:SecureOption", Value = "value" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
            .Setup(x => x.GetMatchingTranslations(searchString))
            .Returns(CreateMatches("TestSecureOption"));

        // User has permission
        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("User has permission to view the setting");

        // Verify permission check was called
        _permissionHelperMock.Verify(
            x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null),
            Times.AtLeastOnce,
            "Permission should be checked for each matched item");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5404")]
    public async Task Search_WhenUserLacksPermission_ShouldExcludeResultFromOutput()
    {
        // Arrange

        // Initialize SettingsSearchPagesProvider with the test assembly containing test configuration classes
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
            typeof(PermissionTestSettings),
        });

        var searchString = "Secure";
        var settings = new List<Setting>
        {
            new() { Key = "PermissionTestSettings:SecureOption", Value = "value" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
           .Setup(x => x.GetMatchingTranslations(searchString))
           .Returns(CreateMatches("TestSecureOption"));

        // User does NOT have permission
        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(false);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty("User lacks permission to view the settings");

        // Verify permission check was still called
        _permissionHelperMock.Verify(
            x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null),
            Times.AtLeastOnce,
            "Permission should be checked even when access is denied");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5404")]
    public async Task Search_WithMixedPermissions_ShouldReturnOnlyAllowedResults()
    {
        // Arrange

        // Initialize SettingsSearchPagesProvider with the test assembly containing test configuration classes
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
            typeof(PermissionTestSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:Option1", Value = "value1" },
            new() { Key = "PermissionTestSettings:SecureOption", Value = "value2" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
           .Setup(x => x.GetMatchingTranslations(searchString))
           .Returns(CreateMatches("TestSecureOption", "TestOption"));

        // Setup permission helper: deny access to items with "SecureOption", allow all others
        SetupPermissionHelper("NoPermission");

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle(r => r.Match == "TestOption",
            "Only results the user has permission to view should be returned");
        result.Should().NotContain(r => r.Match == "TestSecureOption",
            "Results without permission should be excluded");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5404")]
    public async Task Search_WithSpecificPermission_ShouldOnlyReturnItemsWithGrantedPermission()
    {
        // Arrange

        // Initialize SettingsSearchPagesProvider with the test assembly containing test configuration classes
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
            typeof(PermissionTestSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "PermissionTestSettings:SecureOption", Value = "value" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
           .Setup(x => x.GetMatchingTranslations(searchString))
           .Returns(CreateMatches("TestSecureOption", "TestOption"));

        // Setup permission helper to check the specific permission
        SetupPermissionHelper("TestSettingPermission");

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();

        // The permission on PermissionTestSettings is "TestSettingPermission", so it should be included
        result.Should().ContainSingle(r => r.Match == "TestOption",
            "Results without permissions should be returned");
        result.Should().ContainSingle(r => r.Match == "TestSecureOption",
            "Results the user has permission to view should be returned");
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_5326")]
    public async Task Search_WhenPageHasDependency_ShouldExcludeHiddenSettings()
    {
        // Arrange
        // Initialize SettingsSearchPagesProvider with the test assembly containing test configuration classes
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(DependencyTestSettings),
        });


        var searchString = "DependencyTestSettings";
        var settings = new List<Setting>
        {
            new() { Key = "DependencyTestSettings:EnableFeature", Value = "false" },
            new() { Key = "DependencyTestSettings:DependentPage:Option1", Value = "value1" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
          .Setup(x => x.GetMatchingTranslations(searchString))
          .Returns(CreateMatches(
              "DependencyTestSettings_Label_EnableFeature_Label",
              "DependencyTestSettings_Label_Option1_Label",
              "DependencyTestSettings_Label_DependentPage_Label",
              "DependencyTestSettings_Label"
          ));

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainSingle(r => r.Match == "DependencyTestSettings_Label_EnableFeature_Label",
            "Dependent page should be hidden due to dependency on EnableFeature being false");
        // Hidden/disabled pages based on dependencies should not appear in search results
    }

    [Fact]
    public async Task Search_ShouldCreateSearchResultWithCorrectModule()
    {
        // Arrange
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:Option1", Value = "value1" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
          .Setup(x => x.GetMatchingTranslations(searchString))
          .Returns(CreateMatches("Option1"));

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().AllSatisfy(r =>
            r.Module.Should().Be(SearchResultModules.Settings, "All results should be from Settings module"));
    }

    [Fact]
    public async Task Search_ShouldSetStringIdToUrl()
    {
        // Arrange
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:Option1", Value = "value1" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
          .Setup(x => x.GetMatchingTranslations(searchString))
          .Returns(CreateMatches("Option1"));

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().AllSatisfy(r =>
        {
            r.StringId.Should().NotBeNullOrEmpty("StringId should contain the URL");
        });
    }

    [Fact]
    [Trait("ModuleRequirement", "MR_3647")]
    public async Task Search_ResultUrlShouldBeNavigable()
    {
        // Arrange
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:Option1", Value = "value1" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
          .Setup(x => x.GetMatchingTranslations(searchString))
          .Returns(CreateMatches("Option1"));

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().AllSatisfy(r =>
        {
            r.StringId.Should().NotBeNullOrEmpty();
            // URL should be in format like "TestRootSettings_Label" or hierarchical path
            r.StringId.Should().NotContain(":", "URL should use labels, not keys with colons");
        });
    }


    [Fact]
    [Trait("ModuleRequirement", "MR_5399")]
    public async Task Search_WithMultipleMatches_ShouldReturnAllMatchingResults()
    {
        // Arrange
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
        });

        var searchString = "Option";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:Option1", Value = "value1" },
            new() { Key = "TestRootSettings:TestOption", Value = "value2" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
          .Setup(x => x.GetMatchingTranslations(searchString))
          .Returns(CreateMatches("TestOption", "Option1"));

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterOrEqualTo(2, "Multiple matching items should all be returned");
    }

    [Fact]
    public async Task Search_ForProviderManagedPageList_ShouldOnlyUseLoaderReturnedEntries()
    {
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(ProviderManagedSearchSettings),
        });
        SettingsDisplayNameResourceKeyProvider.ResetCache();

        var searchString = "Entry";
        var allSettings = new List<Setting>
        {
            new() { Key = "ProviderManagedSearch:Entries:0:EntryKey", Value = "visible-key" },
            new() { Key = "ProviderManagedSearch:Entries:0:Name", Value = "Visible instrument" },
            new() { Key = "ProviderManagedSearch:Entries:1:EntryKey", Value = "hidden-key" },
            new() { Key = "ProviderManagedSearch:Entries:1:Name", Value = "Hidden instrument" },
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(allSettings);

        var visibleSettings = allSettings
            .Where(setting => setting.Key.Contains(":0:", StringComparison.Ordinal))
            .ToDictionary(setting => setting.Key, setting => setting.Value);
        var visiblePages = LoadPagesForSearch("ProviderManagedSearch", visibleSettings);
        var visiblePageList = visiblePages["ProviderManagedSearch"].Children.OfType<PageList>().Single();
        visiblePageList.PageEntries.Should().ContainSingle();

        _settingsPagesLoaderMock
            .Setup(x => x.LoadAsync("ProviderManagedSearch", It.IsAny<CancellationToken>()))
            .ReturnsAsync(visiblePages);

        _translationSearchProviderMock
            .Setup(x => x.GetMatchingTranslations(searchString))
            .Returns(CreateMatches("ProviderManagedSearchEntryName"));

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var result = await _sut.Search(searchString, TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result[0].StringId.Should().Contain("Visible instrument");
        result[0].StringId.Should().NotContain("Hidden instrument");
        _settingsPagesLoaderMock.Verify(
            x => x.LoadAsync("ProviderManagedSearch", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Search_WhenTranslationKeyIsNotInSettings_ShouldIgnoreMatch()
    {
        // Arrange
        var assembly = GetType().Assembly;
        SettingsSearchPagesProvider.Initialize(new[] { assembly }, _ => new[]
        {
            typeof(TestRootSettings),
        });

        var searchString = "Test";
        var settings = new List<Setting>
        {
            new() { Key = "TestRootSettings:TestOption", Value = "value1" }
        };
        _dbContextMock.Setup(x => x.Settings).ReturnsDbSet(settings);

        _translationSearchProviderMock
            .Setup(x => x.GetMatchingTranslations(searchString))
            .Returns(new List<TranslationSearchMatch>
            {
                new("Unrelated_Module_Key", "TestOption")
            });

        _permissionHelperMock.Setup(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null))
            .ReturnsAsync(true);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _sut.Search(searchString, cancellationToken);

        // Assert
        result.Should().BeEmpty();
        _permissionHelperMock.Verify(x => x.HasPermissionAsync(It.IsAny<SettingItemBase>(), null), Times.Never);
    }
}


[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("TestRootSettings_Label")]
internal class TestRootSettings
{

    [DisplayNameLocal("TestOption")]
    public string TestOption { get; set; } = string.Empty;

    [DisplayNameLocal("Option1")]
    public string Option1 { get; set; } = "default";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("PermissionTestSettings_Label")]
internal class PermissionTestSettings
{

    [DisplayNameLocal("TestSecureOption")]
    [Permission("TestSettingPermission")]
    public string SecureOption { get; set; } = string.Empty;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("GroupTestSettings_Label")]
internal class GroupTestSettings
{

    [DisplayNameLocal("TestGroup_Label")]
    [SettingsGroup]
    public TestGroup TestGroup { get; set; } = new();
}

internal class TestGroup
{
    [DisplayNameLocal("Option1")]
    public string Option1 { get; set; } = "default";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("DependencyTestSettings_Label")]
internal class DependencyTestSettings
{
    [DisplayNameLocal("DependencyTestSettings_Label_EnableFeature_Label")]
    public bool EnableFeature { get; set; }

    [DisplayNameLocal("DependencyTestSettings_Label_DependentPage_Label")]
    [DependsFrom(nameof(EnableFeature), "true", DependencyType.Hide)]
    public DependentPage DependentPage { get; set; } = new();
}

internal class DependentPage
{
    [DisplayNameLocal("DependencyTestSettings_Label_Option1_Label")]
    public string Option1 { get; set; } = "default";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("ProviderManagedSearchSettings_Label")]
internal class ProviderManagedSearchSettings
{
    [DisplayNameLocal("ProviderManagedSearchEntries")]
    [ProviderPageList(nameof(TestSearchPageListEntryProvider), nameof(ProviderManagedSearchEntry.EntryKey))]
    public IEnumerable<ProviderManagedSearchEntry> Entries { get; set; } = [];
}

internal class ProviderManagedSearchEntry
{
    public string EntryKey { get; set; } = "";

    [DisplayNameLocal("ProviderManagedSearchEntryName")]
    public string Name { get; set; } = "";
}

internal sealed class TestSearchPageListEntryProvider : IPageListEntryProvider
{
    public Task<IReadOnlyList<PageListEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PageListEntry>>([]);
    }
}
