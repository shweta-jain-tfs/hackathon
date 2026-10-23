using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Validation;

public class PageListMaxItemsValidatorTests
{
    private readonly Mock<ISettingsDbContext> _mockDbContext;
    private readonly Mock<ISettingsPagesLoader> _mockLoader;
    private readonly IPageTreeNavigator _pageTreeNavigator;
    private readonly PageListMaxItemsValidator _sut;

    public PageListMaxItemsValidatorTests()
    {
        _mockDbContext = new Mock<ISettingsDbContext>();
        _mockLoader = new Mock<ISettingsPagesLoader>();
        _pageTreeNavigator = new PageTreeNavigator();
        _sut = new PageListMaxItemsValidator(_mockDbContext.Object, _mockLoader.Object, _pageTreeNavigator);
    }

    #region ValidateCanAddAsync Tests

    [Fact]
    public async Task ValidateCanAddAsync_Returns_Error_When_At_Max_Limit()
    {
        // Arrange
        var pageListKey = "Lis:LisChannels";
        var rootPageLabel = "Lis";

        var settings = new List<Setting>
        {
            new() { Key = "Lis:LisChannels:0:Name", Value = "Channel1" },
            new() { Key = "Lis:LisChannels:1:Name", Value = "Channel2" },
            new() { Key = "Lis:LisChannels:2:Name", Value = "Channel3" },
            new() { Key = "Lis:LisChannels:3:Name", Value = "Channel4" },
            new() { Key = "Lis:LisChannels:4:Name", Value = "Channel5" },
            new() { Key = "Lis:LisChannels:5:Name", Value = "Channel6" },
            new() { Key = "Lis:LisChannels:6:Name", Value = "Channel7" },
            new() { Key = "Lis:LisChannels:7:Name", Value = "Channel8" },
            new() { Key = "Lis:LisChannels:8:Name", Value = "Channel9" },
            new() { Key = "Lis:LisChannels:9:Name", Value = "Channel10" }
        };

        _mockDbContext.Setup(db => db.Settings).ReturnsDbSet(settings);

        var defaultPage = new Page
        {
            Key = "Lis",
            Children =
            [
                new PageList
                {
                    Key = pageListKey,
                    MaxItems = 10,
                    PageStructure = new Page { Key = "Lis:LisChannels:structure" },
                    PageEntries = []
                }
            ]
        };

        var defaultPages = new Dictionary<string, Page> { ["Lis"] = defaultPage };

        _mockLoader.Setup(l => l.LoadAsync(rootPageLabel, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPages);

        // Act
        var result = await _sut.ValidateCanAddAsync(pageListKey, rootPageLabel, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(10, result.MaxItems);
        Assert.Equal(10, result.CurrentCount);
    }

    [Fact]
    public async Task ValidateCanAddAsync_Returns_Success_When_Below_Max_Limit()
    {
        // Arrange
        var pageListKey = "Lis:LisChannels";
        var rootPageLabel = "Lis";

        var settings = new List<Setting>
        {
            new() { Key = "Lis:LisChannels:0:Name", Value = "Channel1" },
            new() { Key = "Lis:LisChannels:1:Name", Value = "Channel2" }
        };

        _mockDbContext.Setup(db => db.Settings).ReturnsDbSet(settings);

        var defaultPage = new Page
        {
            Key = "Lis",
            Children =
            [
                new PageList
                {
                    Key = pageListKey,
                    MaxItems = 10,
                    PageStructure = new Page { Key = "Lis:LisChannels:structure" },
                    PageEntries = []
                }
            ]
        };

        var defaultPages = new Dictionary<string, Page> { ["Lis"] = defaultPage };

        _mockLoader.Setup(l => l.LoadAsync(rootPageLabel, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPages);

        // Act
        var result = await _sut.ValidateCanAddAsync(pageListKey, rootPageLabel, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateCanAddAsync_Returns_Success_When_MaxItems_Is_Null()
    {
        // Arrange
        var pageListKey = "Lis:LisChannels";
        var rootPageLabel = "Lis";

        var settings = new List<Setting>
        {
            new() { Key = "Lis:LisChannels:0:Name", Value = "Channel1" }
        };

        _mockDbContext.Setup(db => db.Settings).ReturnsDbSet(settings);

        var defaultPage = new Page
        {
            Key = "Lis",
            Children =
            [
                new PageList
                {
                    Key = pageListKey,
                    MaxItems = null, // No limit
                    PageStructure = new Page { Key = "Lis:LisChannels:structure" },
                    PageEntries = []
                }
            ]
        };

        var defaultPages = new Dictionary<string, Page> { ["Lis"] = defaultPage };

        _mockLoader.Setup(l => l.LoadAsync(rootPageLabel, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPages);

        // Act
        var result = await _sut.ValidateCanAddAsync(pageListKey, rootPageLabel, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateExistingPageLists Tests

    [Fact]
    public void ValidateExistingPageLists_Allows_Exactly_At_Limit()
    {
        // Test the >= to > fix: 10/10 should be valid
        // Arrange
        var pageList = new PageList
        {
            Key = "Lis:LisChannels",
            MaxItems = 10,
            PageEntries =
            [
                new Page { Key = "Lis:LisChannels:0" },
                new Page { Key = "Lis:LisChannels:1" },
                new Page { Key = "Lis:LisChannels:2" },
                new Page { Key = "Lis:LisChannels:3" },
                new Page { Key = "Lis:LisChannels:4" },
                new Page { Key = "Lis:LisChannels:5" },
                new Page { Key = "Lis:LisChannels:6" },
                new Page { Key = "Lis:LisChannels:7" },
                new Page { Key = "Lis:LisChannels:8" },
                new Page { Key = "Lis:LisChannels:9" }
            ]
        };

        var page = new Page
        {
            Key = "Lis",
            Children = [pageList]
        };

        var existingKeys = new List<string>();

        // Act
        var result = _sut.ValidateExistingPageLists(page, existingKeys);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateExistingPageLists_Detects_Exceeded_Limit()
    {
        // Arrange
        var pageList = new PageList
        {
            Key = "Lis:LisChannels",
            MaxItems = 10,
            PageEntries = []
        };

        var page = new Page
        {
            Key = "Lis",
            Children = [pageList]
        };

        // 11 existing items in database (exceeds max of 10)
        var existingKeys = new List<string>
        {
            "Lis:LisChannels:0:Name",
            "Lis:LisChannels:1:Name",
            "Lis:LisChannels:2:Name",
            "Lis:LisChannels:3:Name",
            "Lis:LisChannels:4:Name",
            "Lis:LisChannels:5:Name",
            "Lis:LisChannels:6:Name",
            "Lis:LisChannels:7:Name",
            "Lis:LisChannels:8:Name",
            "Lis:LisChannels:9:Name",
            "Lis:LisChannels:10:Name" // 11th item
        };

        // Act
        var result = _sut.ValidateExistingPageLists(page, existingKeys);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Lis:LisChannels", result.Errors[0].Key);
        Assert.Equal(SettingImportError.MaxItemsExceeded, result.Errors[0].Error);
    }

    [Fact]
    public void ValidateExistingPageLists_Ignores_Existing_Keys()
    {
        // Arrange
        var pageList = new PageList
        {
            Key = "Lis:LisChannels",
            MaxItems = 10,
            PageEntries =
            [
                new Page { Key = "Lis:LisChannels:0" },
                new Page { Key = "Lis:LisChannels:1" },
                new Page { Key = "Lis:LisChannels:2" },
                new Page { Key = "Lis:LisChannels:3" },
                new Page { Key = "Lis:LisChannels:4" },
                new Page { Key = "Lis:LisChannels:5" },
                new Page { Key = "Lis:LisChannels:6" },
                new Page { Key = "Lis:LisChannels:7" },
                new Page { Key = "Lis:LisChannels:8" },
                new Page { Key = "Lis:LisChannels:9" },
                new Page { Key = "Lis:LisChannels:10" } // 11 items total
            ]
        };

        var page = new Page
        {
            Key = "Lis",
            Children = [pageList]
        };

        // Mark one item as existing in DB, so only 10 are "new"
        var existingKeys = new List<string> { "Lis:LisChannels:0" };

        // Act
        var result = _sut.ValidateExistingPageLists(page, existingKeys);

        // Assert - Should succeed because only 10 items are new
        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidateImportedPageLists Tests

    [Fact]
    public void ValidateImportedPageLists_Detects_Exceeded_Limit_From_CSV_Keys()
    {
        // Test the fix for counting CSV import keys
        // Arrange
        var pageWithFlatOptions = new Page
        {
            Key = "Lis",
            Children =
            [
                // CSV import creates flat Option list, not populated PageList
                new Option { Key = "Lis:LisChannels:0:Name", Value = "Channel1" },
                new Option { Key = "Lis:LisChannels:1:Name", Value = "Channel2" },
                new Option { Key = "Lis:LisChannels:2:Name", Value = "Channel3" },
                new Option { Key = "Lis:LisChannels:3:Name", Value = "Channel4" },
                new Option { Key = "Lis:LisChannels:4:Name", Value = "Channel5" },
                new Option { Key = "Lis:LisChannels:5:Name", Value = "Channel6" },
                new Option { Key = "Lis:LisChannels:6:Name", Value = "Channel7" },
                new Option { Key = "Lis:LisChannels:7:Name", Value = "Channel8" },
                new Option { Key = "Lis:LisChannels:8:Name", Value = "Channel9" },
                new Option { Key = "Lis:LisChannels:9:Name", Value = "Channel10" },
                new Option { Key = "Lis:LisChannels:10:Name", Value = "Channel11" } // 11 items
            ]
        };

        // Mock loader to return page structure with MaxItems
        var defaultPage = new Page
        {
            Key = "Lis",
            Children =
            [
                new PageList
                {
                    Key = "Lis:LisChannels",
                    MaxItems = 10,
                    PageStructure = new Page { Key = "Lis:LisChannels:structure" },
                    PageEntries = []
                }
            ]
        };

        var defaultPages = new Dictionary<string, Page>
        {
            ["Lis"] = defaultPage
        };

        _mockLoader.Setup(l => l.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPages);

        // Act
        var result = _sut.ValidateImportedPageLists(pageWithFlatOptions);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Lis:LisChannels", result.Errors[0].Key);
        Assert.Equal("11", result.Errors[0].Value);
        Assert.Equal(SettingImportError.MaxItemsExceeded, result.Errors[0].Error);
    }

    [Fact]
    public void ValidateImportedPageLists_Allows_Exactly_At_Limit()
    {
        // Test the >= to > fix for import
        // Arrange
        var pageWithFlatOptions = new Page
        {
            Key = "Lis",
            Children =
            [
                new Option { Key = "Lis:LisChannels:0:Name", Value = "Channel1" },
                new Option { Key = "Lis:LisChannels:1:Name", Value = "Channel2" },
                new Option { Key = "Lis:LisChannels:2:Name", Value = "Channel3" },
                new Option { Key = "Lis:LisChannels:3:Name", Value = "Channel4" },
                new Option { Key = "Lis:LisChannels:4:Name", Value = "Channel5" },
                new Option { Key = "Lis:LisChannels:5:Name", Value = "Channel6" },
                new Option { Key = "Lis:LisChannels:6:Name", Value = "Channel7" },
                new Option { Key = "Lis:LisChannels:7:Name", Value = "Channel8" },
                new Option { Key = "Lis:LisChannels:8:Name", Value = "Channel9" },
                new Option { Key = "Lis:LisChannels:9:Name", Value = "Channel10" } // Exactly 10 items
            ]
        };

        var defaultPage = new Page
        {
            Key = "Lis",
            Children =
            [
                new PageList
                {
                    Key = "Lis:LisChannels",
                    MaxItems = 10,
                    PageStructure = new Page { Key = "Lis:LisChannels:structure" },
                    PageEntries = []
                }
            ]
        };

        var defaultPages = new Dictionary<string, Page>
        {
            ["Lis"] = defaultPage
        };

        _mockLoader.Setup(l => l.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPages);

        // Act
        var result = _sut.ValidateImportedPageLists(pageWithFlatOptions);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateImportedPageLists_Returns_Success_When_No_PageLists()
    {
        // Arrange
        var pageWithoutPageLists = new Page
        {
            Key = "Simple",
            Children =
            [
                new Option { Key = "Simple:Option1", Value = "Value1" },
                new Option { Key = "Simple:Option2", Value = "Value2" }
            ]
        };

        var defaultPages = new Dictionary<string, Page>
        {
            ["Simple"] = new Page
            {
                Key = "Simple",
                Children =
                [
                    new Option { Key = "Simple:Option1" },
                    new Option { Key = "Simple:Option2" }
                ]
            }
        };

        _mockLoader.Setup(l => l.LoadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPages);

        // Act
        var result = _sut.ValidateImportedPageLists(pageWithoutPageLists);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion
}
