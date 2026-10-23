using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Services;
public class PageListPopulatorTests
{
    private readonly PageListPopulator _pageListPopulator;

    public PageListPopulatorTests()
    {
        _pageListPopulator = new PageListPopulator();
    }

    [Fact]
    public void Populate_ShouldThrowInvalidOperationException_WhenPageStructureIsNull()
    {
        // Arrange
        var pageList = new PageList { PageStructure = null };
        var component = (SettingItemBase)pageList;
        var settings = new Dictionary<string, string?>();
        var populatedPages = new Dictionary<string, Page>();

        // Act
        var act = () => _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("PageList does not have a page structure!");
    }

    [Fact]
    public void Populate_ShouldAddPageStructureToPopulatedPages_WhenPageStructureIsNotNull()
    {
        // Arrange
        var pageList = new PageList
        {
            PageStructure = new Page { Key = "Page1", Url = "Root" },
            PageEntries = new List<ChildPageItem>()
        };
        var component = (SettingItemBase)pageList;
        var settings = new Dictionary<string, string?>();
        var populatedPages = new Dictionary<string, Page>();

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        populatedPages.Should().ContainKey("Page1");
    }

    [Fact]
    public void Populate_ShouldPopulateExistingPageEntries()
    {
        // Arrange
        var existingPage = new Page { Key = "Page1", Url = "Root" };
        var pageList = new PageList
        {
            PageStructure = new Page { Key = "Page1", Url = "Root" },
            PageEntries = [existingPage]
        };
        var component = (SettingItemBase)pageList;
        var settings = new Dictionary<string, string?>();
        var populatedPages = new Dictionary<string, Page> { { "Page1", existingPage } };

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        populatedPages.Should().ContainKey("Page1");
    }

    [Fact]
    public void Populate_ShouldCreateNewPageEntries()
    {
        // Arrange
        var pageStructure = new Page
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList/EntryProperty",
            Children =
            [
                new Option
                {
                    Type = OptionType.String,
                    Key = "mainPage:PageList:Name"
                }
            ],
            Header = "Header"
        };

        var pageList = new PageList
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList",
            PageStructure = pageStructure,
            PageEntries = new List<ChildPageItem>(),
        };

        var component = (SettingItemBase)pageList;

        var settings = new Dictionary<string, string?>
       {
           { "mainPage:PageList:0:Name", "Label1" },
           { "mainPage:PageList:1:Name", "Label2" }
       };

        var populatedPages = new Dictionary<string, Page>();

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        populatedPages.Should().HaveCount(3);

        var first = populatedPages["mainPage:PageList:0"];
        first.Children.First().As<Option>().Value.Should().Be("Label1");
        first.Children.First().As<Option>().Key.Should().Be("mainPage:PageList:0:Name");
        var second = populatedPages["mainPage:PageList:1"];
        second.Children.First().As<Option>().Value.Should().Be("Label2");
        second.Children.First().As<Option>().Key.Should().Be("mainPage:PageList:1:Name");
    }

    [Fact]
    public void Populate_ShouldCreateProviderManagedEntriesWithoutNameProperty()
    {
        // Arrange
        var pageStructure = new Page
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList/EntryProperty",
            Children =
            [
                new Option
                {
                    Type = OptionType.String,
                    Key = "mainPage:PageList:EntryKey",
                    IsHidden = true
                },
                new Option
                {
                    Type = OptionType.Boolean,
                    Key = "mainPage:PageList:Enabled",
                    Label = "Enabled",
                    IsColumnHeader = true
                }
            ],
            Header = "Header"
        };

        var pageList = new PageList
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList",
            IsProviderManaged = true,
            ProviderKeyPropertyName = "EntryKey",
            PageStructure = pageStructure,
            PageEntries = []
        };

        var settings = new Dictionary<string, string?>
        {
            { "mainPage:PageList:0:EntryKey", "SN-1" },
            { "mainPage:PageList:0:Enabled", "true" }
        };

        var component = (SettingItemBase)pageList;
        var populatedPages = new Dictionary<string, Page>();

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        var entry = populatedPages["mainPage:PageList:0"];
        entry.Label.Should().Be("SN-1");
        entry.Children.OfType<Option>().Single(o => o.Key == "mainPage:PageList:0:EntryKey").Value.Should().Be("SN-1");
        entry.Children.OfType<Option>().Single(o => o.Key == "mainPage:PageList:0:Enabled").Value.Should().Be("true");
        pageList.Columns.Should().ContainSingle(c => c.Value!.Key == "mainPage:PageList:0:Enabled");
    }

    [Fact]
    public void Populate_ShouldThrowInvalidOperationException_WhenPageStructureIsNotPage()
    {
        // Arrange
        var pageList = new PageList
        {
            PageStructure = new ChildPageItem { Key = "Option1" },
            PageEntries = new List<ChildPageItem>()
        };
        var component = (SettingItemBase)pageList;
        var settings = new Dictionary<string, string?>();
        var populatedPages = new Dictionary<string, Page>();

        // Act
        var act = () => _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Populate_ShouldCreateDependencyKeysCorrectly()
    {
        // Arrange
        var pageStructure = new Page
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList/EntryProperty",
            Children =
            [
                new Option
                        {
                            Type = OptionType.String,
                            Key = "mainPage:PageList:Name",
                },
                new Group{
                    Key = "mainPage:PageList:Group",
                    Children=
                    [
                        new Option
                        {
                            Type = OptionType.Boolean,
                            Key = "mainPage:PageList:Group:Bool"
                        },
                        new Option
                        {
                            Type = OptionType.String,
                            Key = "mainPage:PageList:Group:Name",
                            Dependency =
                            [
                                new Dependency
                                {
                                    MainOptionValue = "True",
                                    MainOptionKey = "mainPage:PageList:Group:Bool",
                                    Type = DependencyType.Hide
                                }
                            ]
                        }
                    ]
                },
            ],
            Header = "Header"
        };

        var pageList = new PageList
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList",
            PageStructure = pageStructure,
            PageEntries = new List<ChildPageItem>(),
        };

        var component = (SettingItemBase)pageList;

        var settings = new Dictionary<string, string?>
       {
            { "mainPage:PageList:0:Name", "Name" },
            { "mainPage:PageList:0:Group:Bool", "False" },
           { "mainPage:PageList:0:Group:Name", "Dependent" },
       };

        var populatedPages = new Dictionary<string, Page>();

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        populatedPages.Should().HaveCount(3);

        var first = populatedPages["mainPage:PageList:0"];
        var children = first.Children.ToList();
        first.Children.First().As<Option>().Value.Should().Be("Name");
        first.Children.First().As<Option>().Key.Should().Be("mainPage:PageList:0:Name");

        children[1].Should().BeOfType<Group>();
        var group = children[1].As<Group>();
        group.Should().NotBeNull();
        var dependendantOption = group.Children[1].As<Option>();
        dependendantOption.Dependency.Should().NotBeNull();
        var dependencies = dependendantOption.Dependency;
        dependencies.Should().HaveCount(1);
        var dependency = dependencies[0];
        dependency.MainOptionValue.Should().Be("True");
        dependency.MainOptionKey.Should().Be("mainPage:PageList:0:Group:Bool");
    }

    [Fact]
    public void Populate_ShouldNotDuplicateExistingEntry_WhenExistingKeyUsesParentIndexFormat()
    {
        // Arrange
        var pageStructure = new Page
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList/EntryProperty",
            Children =
            [
                new Option
                {
                    Type = OptionType.String,
                    Key = "mainPage:PageList:Name"
                }
            ],
            Header = "Header"
        };

        var existingEntry = new Page
        {
            Key = "mainPage:PageList:5",
            Url = "mainPage/PageList/EntryProperty",
            Label = "ExistingLabel",
            Children =
            [
                new Option
                {
                    Type = OptionType.String,
                    Key = "mainPage:PageList:5:Name",
                    Value = "ExistingLabel"
                }
            ]
        };

        var pageList = new PageList
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList",
            PageStructure = pageStructure,
            PageEntries = [existingEntry]
        };

        var settings = new Dictionary<string, string?>
        {
            { "mainPage:PageList:5:Name", "ExistingLabel" }
        };

        var component = (SettingItemBase)pageList;
        var populatedPages = new Dictionary<string, Page>();

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        pageList.PageEntries.OfType<Page>().Should().HaveCount(1);
        pageList.PageEntries.OfType<Page>().Count(p => p.Key == "mainPage:PageList:5").Should().Be(1);
    }

    [Fact]
    public void Populate_ShouldNotDuplicateDbEntries_WhenCalledMultipleTimes_WithSparseIndices()
    {
        // Arrange
        var pageStructure = new Page
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList/EntryProperty",
            Children =
            [
                new Option
                {
                    Type = OptionType.String,
                    Key = "mainPage:PageList:Name"
                }
            ],
            Header = "Header"
        };

        var pageList = new PageList
        {
            Key = "mainPage:PageList",
            Url = "mainPage/PageList",
            PageStructure = pageStructure,
            PageEntries = []
        };

        var settings = new Dictionary<string, string?>
        {
            { "mainPage:PageList:1:Name", "Label1" },
            { "mainPage:PageList:5:Name", "Label5" }
        };

        var component = (SettingItemBase)pageList;
        var populatedPages = new Dictionary<string, Page>();

        // Act
        _pageListPopulator.Populate(component, settings, ref populatedPages);
        _pageListPopulator.Populate(component, settings, ref populatedPages);

        // Assert
        var pageEntries = pageList.PageEntries.OfType<Page>().ToList();
        pageEntries.Should().HaveCount(2);
        pageEntries.Select(p => p.Key).Should().BeEquivalentTo(["mainPage:PageList:1", "mainPage:PageList:5"]);
    }
}
