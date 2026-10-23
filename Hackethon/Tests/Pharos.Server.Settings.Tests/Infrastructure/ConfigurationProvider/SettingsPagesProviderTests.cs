using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Common;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Domain;
using Pharos.Server.Settings.Helpers;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

public class SettingsPagesProviderTests : IDisposable
{
    private readonly ISettingsStringLocalizer _localizer;

    public SettingsPagesProviderTests()
    {
        BaseSettingsPagesProvider.Initialize(new[] { GetType().Assembly });

        var mockLocalizer = new Mock<ISettingsStringLocalizer>();
        mockLocalizer
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => new LocalizedString(key, key));

        _localizer = mockLocalizer.Object;
    }

    public void Dispose()
    {
        BaseSettingsPagesProvider.Initialize([]);
    }

    [Fact]
    public void CreateComplexTestStructure()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ComplexSettingsTest) });

        pages.Should().HaveCount(7);
    }

    [Fact]
    public void BasicPageShouldHaveCorrectOptions()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(BasicPage) }).FirstOrDefault();

        var page = pages.Value;

        page.Children.Should().HaveCount(3);
        page.Key.Should().Be("BasicPage");
        page.Label.Should().Be("RootLabel");
        page.Header.Should().Be("General");

        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Value.Should().Be("");
        page.Children[0].As<Option>().Key.Should().Be("BasicPage:StringSetting");
        page.Children[0].As<Option>().Label.Should().Be("String Label");
        page.Children[0].As<Option>().Type.Should().Be(OptionType.String);

        page.Children[1].Should().BeOfType<Option>();
        page.Children[1].As<Option>().Value.Should().Be("0");
        page.Children[1].As<Option>().MaxValue.Should().Be("2147483647");
        page.Children[1].As<Option>().MinValue.Should().Be("-2147483648");
        page.Children[1].As<Option>().Key.Should().Be("BasicPage:IntSetting");
        page.Children[1].As<Option>().Label.Should().Be("Int Label");
        page.Children[1].As<Option>().Type.Should().Be(OptionType.Integer);

        page.Children[2].Should().BeOfType<Option>();
        page.Children[2].As<Option>().Value.Should().Be("Portrait");
        page.Children[2].As<Option>().Key.Should().Be("BasicPage:PageOrientation");
        page.Children[2].As<Option>().Label.Should().Be("PageOrientation");
        page.Children[2].As<Option>().Type.Should().Be(OptionType.Choice);
        page.Children[2].As<Option>().Choices.Should().HaveCount(2);
        page.Children[2].As<Option>().Choices[0].Label.Should().Be("PageOrientation_Portrait");
        page.Children[2].As<Option>().Choices[0].Value.Should().Be("Portrait");
        page.Children[2].As<Option>().Choices[1].Label.Should().Be("PageOrientation_Landscape");
        page.Children[2].As<Option>().Choices[1].Value.Should().Be("Landscape");
    }

    [Fact]
    public void DependencyShouldWorkCorrectly()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(CorrectDependencyPage) }).FirstOrDefault()!;

        var page = pages.Value;

        page.Children.Should().NotBeNull();
        page.Children.Count.Should().Be(2);
        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Dependency.Should().NotBeNull();
        page.Children[0].As<Option>().Dependency.Should().BeEmpty();

        page.Children[1].Should().BeOfType<Option>();
        page.Children[1].As<Option>().Dependency.Should().NotBeNull();
        page.Children[1].As<Option>().Dependency?.Count.Should().Be(1);
        page.Children[1].As<Option>().Dependency?.First().MainOptionValue.Should().Be("false");
        page.Children[1].As<Option>().Dependency?.First().MainOptionKey.Should().Be("CorrectDependencyPage:Depend");
        page.Children[1].As<Option>().Dependency?.First().Type.Should().Be(DependencyType.Hide);
    }

    [Fact]
    [Trait("ProductRequirement", "PR_2803")]
    public void TubeBarcodeChecksumOptions_ShouldHaveDisableDependenciesOnTheirBarcodeTypes()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(SamplesSettings) });
        var options = pages
            .SelectMany(page => PageHelper.FindOptions(page.Value).Values)
            .DistinctBy(option => option.Key)
            .ToList();

        AssertDisableDependency(
            options.Single(option => option.Key.EndsWith(":Code39Checksum", StringComparison.Ordinal)),
            "Code39");
        AssertDisableDependency(
            options.Single(option => option.Key.EndsWith(":Interleaved2Of5Checksum", StringComparison.Ordinal)),
            "Interleaved2Of5");
    }

    private static void AssertDisableDependency(Option option, string expectedParentProperty)
    {
        var parentKeyPrefix = option.Key[..(option.Key.LastIndexOf(':') + 1)];

        option.Dependency.Should().ContainSingle();
        option.Dependency.Single().MainOptionKey.Should().Be(parentKeyPrefix + expectedParentProperty);
        option.Dependency.Single().MainOptionValue.Should().Be("True");
        option.Dependency.Single().Type.Should().Be(DependencyType.Disable);
    }

    [Trait("ProductRequirement", "PR_1919")]
    [Trait("ModuleRequirement", "MR_3926")]
    [Fact]
    public void NonOverlappingRangesAttribute_ShouldAddValidationMetadataToRangeOptions()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(NonOverlappingRangesPage) }).FirstOrDefault()!;
        var options = PageHelper.FindOptions(pages.Value);
        var rangeKeys = new[]
        {
            "NonOverlappingRangesPage:FirstStart",
            "NonOverlappingRangesPage:FirstLength",
            "NonOverlappingRangesPage:SecondStart",
            "NonOverlappingRangesPage:SecondLength"
        };

        foreach (var key in rangeKeys)
        {
            options[key].NonOverlappingRanges.Should().ContainSingle();
        }

        var validation = options["NonOverlappingRangesPage:FirstStart"].NonOverlappingRanges.Single();
        validation.RangeKeys.Should().BeEquivalentTo(rangeKeys);
        validation.EnabledWhen.Should().ContainSingle();
        validation.EnabledWhen.Single().MainOptionKey.Should().Be("NonOverlappingRangesPage:Enabled");
        validation.EnabledWhen.Single().MainOptionValue.Should().Be("True");
    }

    [Fact]
    public void DependencyShouldWorkCorrectlyForPages()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(CorrectPageDependencyPage) }).FirstOrDefault()!;

        var page = pages.Value;

        page.Children.Should().NotBeNull();
        page.Children.Count.Should().Be(2);
        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Dependency.Should().NotBeNull();
        page.Children[0].As<Option>().Dependency.Should().BeEmpty();

        page.Children[1].Should().BeOfType<Page>();
        page.Children[1].As<Page>().Dependency.Should().NotBeNull();
        page.Children[1].As<Page>().Dependency?.Count.Should().Be(1);
        page.Children[1].As<Page>().Dependency?.First().MainOptionValue.Should().Be("false");
        page.Children[1].As<Page>().Dependency?.First().MainOptionKey.Should().Be("CorrectPageDependencyPage:Depend");
        page.Children[1].As<Page>().Dependency?.First().Type.Should().Be(DependencyType.Hide);
    }

    [Fact]
    public void DependencyShouldWorkCorrectlyForGroups()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(CorrectGroupDependencyPage) }).FirstOrDefault()!;

        var page = pages.Value;

        page.Children.Should().NotBeNull();
        page.Children.Count.Should().Be(2);
        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Dependency.Should().NotBeNull();
        page.Children[0].As<Option>().Dependency.Should().BeEmpty();

        page.Children[1].Should().BeOfType<Group>();
        page.Children[1].As<Group>().Dependency.Should().NotBeNull();
        page.Children[1].As<Group>().Dependency?.Count.Should().Be(1);
        page.Children[1].As<Group>().Dependency?.First().MainOptionValue.Should().Be("false");
        page.Children[1].As<Group>().Dependency?.First().MainOptionKey.Should().Be("CorrectGroupDependencyPage:Depend");
        page.Children[1].As<Group>().Dependency?.First().Type.Should().Be(DependencyType.Hide);
    }

    [Fact]
    public void MultipleDependencyShouldWorkCorrectly()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(CorrectMultipleDependencyPage) }).FirstOrDefault()!;

        var page = pages.Value;

        page.Children.Should().NotBeNull();
        page.Children.Count.Should().Be(3);
        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Dependency.Should().NotBeNull();
        page.Children[0].As<Option>().Dependency.Should().BeEmpty();

        page.Children[1].Should().BeOfType<Option>();
        page.Children[1].As<Option>().Dependency.Should().NotBeNull();
        page.Children[1].As<Option>().Dependency.Should().BeEmpty();

        page.Children[2].Should().BeOfType<Option>();
        page.Children[2].As<Option>().Dependency.Should().NotBeNull();
        page.Children[2].As<Option>().Dependency?.Count.Should().Be(2);
        page.Children[2].As<Option>().Dependency?[0].MainOptionValue.Should().Be("false");
        page.Children[2].As<Option>().Dependency?[0].MainOptionKey.Should().Be("CorrectMultipleDependencyPage:Depend");
        page.Children[2].As<Option>().Dependency?[0].Type.Should().Be(DependencyType.Disable);
        page.Children[2].As<Option>().Dependency?[1].MainOptionValue.Should().Be("full");
        page.Children[2].As<Option>().Dependency?[1].MainOptionKey.Should().Be("CorrectMultipleDependencyPage:Depend2");
        page.Children[2].As<Option>().Dependency?[1].Type.Should().Be(DependencyType.Disable);
    }

    [Fact]
    public void DependencyShouldThrowCorrectErrorWithIncorrectScope()
    {
        // Act and assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(IncorrectDependencyPage) });
        });
    }

    [Fact]
    public void DependencyShouldThrowCorrectErrorWithIncorrectOrder()
    {
        // Act and assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(IncorrectDependencyOrderPage) });
        });
    }

    [Fact]
    public void DependencyShouldThrowCorrectErrorWithIncorrectDependencyDefinition()
    {
        // Act and assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(IncorrectDependencyDefinitionPage) });
        });

        Assert.Equal($"Property or Value cannot be empty list in '{nameof(IncorrectDependencyDefinitionPage.StringSetting)}' DependsFromAttribute", exception.Message);
    }

    [Fact]
    public void DependencyShouldThrowCorrectErrorWithIncorrectMultipleDependencyValueDefinition()
    {
        // Act and assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(IncorrectMultipleDependencyValuePage) });
        });
    }

    [Fact]
    public void DependencyShouldThrowCorrectErrorWithIncorrectMultipleDependencyPropertyDefinition()
    {
        // Act and assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(IncorrectMultipleDependencyPropertyPage) });
        });
    }

    [Fact]
    public void PageListSubPageShouldBeInitializedCorrectly()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ComplexPageListPage) }).FirstOrDefault();

        var page = pages.Value;

        page.Children[0].Should().BeOfType<PageList>();
        page.Children[0].As<PageList>().PageEntries.Should().HaveCount(2);

        //Test Page structure
        page.Children[0].As<PageList>().PageStructure.Should().NotBe(null);
        page.Children[0].As<PageList>().PageStructure.As<Page>().Should().BeOfType<Page>();
        page.Children[0].As<PageList>().PageStructure.As<Page>().Key.Should().Be("ComplexPageListPage:ComplexComponents");

        page.Children[0].As<PageList>().PageStructure.As<Page>().Children.Should().HaveCount(4);
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[0].Should().BeOfType<Option>();
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:Name");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[0].As<Option>().Value.Should().Be("");

        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[1].Should().BeOfType<Option>();
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[1].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:MainComponent");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[1].As<Option>().Value.Should().Be("Default Main Component Value");

        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[2].Should().BeOfType<Page>();
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[2].As<Page>().Key.Should().Be("ComplexPageListPage:ComplexComponents:SubPage");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[2].As<Page>().Label.Should().Be("SubPage");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[2].As<Page>().Children.Should().HaveCount(2);
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[2].As<Page>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:SubPage:BasicString");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[2].As<Page>().Children[0].As<Option>().Value.Should().Be("Default String Value");

        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[3].Should().BeOfType<Group>();
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[3].As<Group>().Key.Should().Be("ComplexPageListPage:ComplexComponents:SubGroup");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[3].As<Group>().Label.Should().Be("SubGroup");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[3].As<Group>().Children.Should().HaveCount(1);
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[3].As<Group>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:SubGroup:BasicString");
        page.Children[0].As<PageList>().PageStructure.As<Page>().Children[3].As<Group>().Children[0].As<Option>().Value.Should().Be("Default String Value");


        //Test initialized Page entry
        page.Children[0].As<PageList>().PageEntries[0].Should().BeOfType<Page>();
        page.Children[0].As<PageList>().PageEntries[0].Key.Should().Be("ComplexPageListPage:ComplexComponents:0");

        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children.Should().HaveCount(4);
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[0].Should().BeOfType<Option>();
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:0:Name");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[0].As<Option>().Value.Should().Be("Initialized Name");

        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[1].Should().BeOfType<Option>();
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[1].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:0:MainComponent");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[1].As<Option>().Value.Should().Be("Initialized Value");

        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[2].Should().BeOfType<Page>();
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[2].As<Page>().Key.Should().Be("ComplexPageListPage:ComplexComponents:0:SubPage");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[2].As<Page>().Children.Should().HaveCount(2);
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[2].As<Page>().Label.Should().Be("SubPage");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[2].As<Page>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:0:SubPage:BasicString");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[2].As<Page>().Children[0].As<Option>().Value.Should().Be("SubPage Initialized Value");

        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[3].Should().BeOfType<Group>();
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[3].As<Group>().Key.Should().Be("ComplexPageListPage:ComplexComponents:0:SubGroup");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[3].As<Group>().Label.Should().Be("SubGroup");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[3].As<Group>().Children.Should().HaveCount(1);
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[3].As<Group>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:0:SubGroup:BasicString");
        page.Children[0].As<PageList>().PageEntries[0].As<Page>().Children[3].As<Group>().Children[0].As<Option>().Value.Should().Be("SubGroup Initialized Value");

        //Test uninitialized Page entry
        page.Children[0].As<PageList>().PageEntries[1].Should().BeOfType<Page>();
        page.Children[0].As<PageList>().PageEntries[1].Key.Should().Be("ComplexPageListPage:ComplexComponents:1");

        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children.Should().HaveCount(4);
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[0].Should().BeOfType<Option>();
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:1:Name");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[0].As<Option>().Value.Should().Be("");

        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[1].Should().BeOfType<Option>();
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[1].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:1:MainComponent");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[1].As<Option>().Value.Should().Be("Default Main Component Value");

        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[2].Should().BeOfType<Page>();
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[2].As<Page>().Key.Should().Be("ComplexPageListPage:ComplexComponents:1:SubPage");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[2].As<Page>().Label.Should().Be("SubPage");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[2].As<Page>().Children.Should().HaveCount(2);
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[2].As<Page>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:1:SubPage:BasicString");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[2].As<Page>().Children[0].As<Option>().Value.Should().Be("Default String Value");

        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[3].Should().BeOfType<Group>();
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[3].As<Group>().Key.Should().Be("ComplexPageListPage:ComplexComponents:1:SubGroup");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[3].As<Group>().Label.Should().Be("SubGroup");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[3].As<Group>().Children.Should().HaveCount(1);
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[3].As<Group>().Children[0].As<Option>().Key.Should().Be("ComplexPageListPage:ComplexComponents:1:SubGroup:BasicString");
        page.Children[0].As<PageList>().PageEntries[1].As<Page>().Children[3].As<Group>().Children[0].As<Option>().Value.Should().Be("Default String Value");
    }

    [Fact]
    public void PageList_Should_Support_TableColumn_In_Nested_Page_Or_Group()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(NestedTableColumnPageListRoot) }).FirstOrDefault();
        var page = pages.Value;

        var pageList = page.Children.OfType<PageList>().Single();

        pageList.ColumnHeaderLabels.Should().Equal("Name", "NestedColumn");

        pageList.Columns.Clear();
        var entry = pageList.PageEntries.OfType<Page>().Single();
        PageListColumnHelper.ExtractAndAddColumns(pageList, entry);

        pageList.Columns.Should().ContainSingle(row =>
            row.ColumnHeaderLabel == "NestedColumn"
            && row.Value!.Key == "NestedTableColumnPageListRoot:Entries:0:Details:NestedColumn"
            && row.Value.Value == "Nested value");
    }

    [Fact]
    public void GroupShouldHaveCorrectValues()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(GroupPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(2);
        page.Children[0].Should().BeOfType<Group>();
        page.Children[0].As<Group>().Label.Should().Be("GroupOne Label");
        page.Children[0].As<Group>().Key.Should().Be("GroupPage:GroupOne");
        page.Children[0].As<Group>().Children.Should().HaveCount(1);
        page.Children[0].As<Group>().Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Group>().Children[0].As<Option>().Key.Should().Be("GroupPage:GroupOne:BasicString");
        page.Children[0].As<Group>().Children[0].As<Option>().Value.Should().Be("Default String Value");

        page.Children[1].As<Group>().Label.Should().Be("GroupTwo Label");
        page.Children[1].As<Group>().Key.Should().Be("GroupPage:GroupTwo");
        page.Children[1].As<Group>().Children.Should().HaveCount(1);
        page.Children[1].As<Group>().Children[0].Should().BeOfType<Option>();
        page.Children[1].As<Group>().Children[0].As<Option>().Key.Should().Be("GroupPage:GroupTwo:BasicString");
        page.Children[1].As<Group>().Children[0].As<Option>().Value.Should().Be("Initialized Group");
    }

    [Fact]
    public void PageShouldHaveCorrectLabels()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageWithSubPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(2);
        page.Key.Should().Be("PageWithSubPage");
        page.Header.Should().Be("Header");
        page.Label.Should().Be("TopPage Label");

        page.Children[0].Should().BeOfType<Page>();
        page.Children[0].As<Page>().Label.Should().Be("SubPage Label");
        page.Children[0].As<Page>().Key.Should().Be("PageWithSubPage:SubPageOne");

        page.Children[1].Should().BeOfType<Page>();
        page.Children[1].As<Page>().Label.Should().Be("SubPageTwo Label");
        page.Children[1].As<Page>().Key.Should().Be("PageWithSubPage:SubPageTwo");
    }

    [Fact]
    public void HelpTextDefaultValueShouldBeEmpty()
    {
        var viewModel = new SettingItemBase();
        var value = viewModel.LabelHelpText;
        Assert.Empty(value);
    }

    [Fact]
    public void HelpTextSetValueShouldReturnCorrectValue()
    {
        var viewModel = new SettingItemBase();
        var expectedValue = "Help Text Label";

        viewModel.LabelHelpText = expectedValue;
        var value = viewModel.LabelHelpText;
        Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void IncorrectDynamicDropdownShouldThrowCorrectError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(IncorrectDynamicDropdownPage) });
        });
    }

    [Fact]
    public void DynamicDropdownShouldBeInitializedCorrectly()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(DynamicDropdownPage) }).FirstOrDefault(); ;
        var page = pages.Value;

        page.Children.Should().HaveCount(2);
        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Label.Should().Be("DynamicDropdown_Test_Label");
        page.Children[0].As<Option>().DynamicDropdownValueTarget.Should().Be("MainComponent");
        page.Children[0].As<Option>().DynamicDropdownOrigin.Should().Be("ComplexComponents");

    }

    [Fact]
    public void MissingRootDisplayNameShouldThrowCorrectError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(MissingRootDisplayName) });
        });
    }

    [Fact]
    public void MissingSettingDisplayNameShouldThrowCorrectError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(MissingSettingDisplayName) });
        });
    }

    [Fact]
    public void MissingPageDisplayNameShouldThrowCorrectError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(MissingPageDisplayName) });
        });
    }

    [Fact]
    public void MissingGroupDisplayNameShouldThrowCorrectError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(MissingGroupDisplayName) });
        });
    }

    [Fact]
    public void MissingPageListDisplayNameShouldThrowCorrectError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(MissingPageListDisplayName) });
        });
    }

    [Fact]
    public async Task GetMenuItemsAsync_Filters_By_Permission()
    {
        // simulate permission check
        var assembly = typeof(RootConfigSectionAttribute).Assembly;
        SettingsPagesProvider.Initialize([assembly]);
        Task<bool> HasPermissions(params string[] permissions) => Task.FromResult(!permissions.Contains("DenyAccess"));

        var result = await SettingsPagesProvider.GetMenuItemsAsync(_localizer, HasPermissions);

        Assert.All(result, kvp => Assert.NotEqual("DenyAccess", kvp.Key));
    }

    [Fact]
    public void DependencyShouldBeCorrectInPageList()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(DependencyInPageList) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(2);
        page.Children[0].Should().BeOfType<PageList>();

        var pageList = page.Children[0] as PageList;
        pageList.Should().NotBeNull();
        pageList.PageEntries.Should().HaveCount(1);
        pageList.PageEntries[0].Should().NotBeNull();
        pageList.PageEntries[0].Should().BeOfType<Page>();

        var pageEntry = pageList.PageEntries[0] as Page;
        pageEntry.Should().NotBeNull();
        pageEntry.Children.Should().HaveCount(4);
        pageEntry.Children[1].Key.Should().Be("DependencyInPageList:DependencyComponents:0:Parent");

        pageEntry.Children[2].Dependency.Should().NotBeEmpty();
        pageEntry.Children[2].Key.Should().Be("DependencyInPageList:DependencyComponents:0:MainComponent");
        pageEntry.Children[2].Dependency.Should().HaveCount(1);
        var dependecy = pageEntry.Children[2].Dependency[0];
        dependecy.Should().NotBeNull();
        dependecy.MainOptionKey.Should().Be("DependencyInPageList:DependencyComponents:0:Parent");

        pageList.PageStructure.Should().NotBeNull();
        var pageStructure = pageList.PageStructure as Page;
        pageStructure.Should().NotBeNull();
        pageStructure.Children[2].Dependency.Should().NotBeEmpty();
        pageStructure.Children[2].Key.Should().Be("DependencyInPageList:DependencyComponents:MainComponent");
        pageStructure.Children[2].Dependency.Should().HaveCount(1);
        var dependecyStructure = pageStructure.Children[2].Dependency[0];
        dependecyStructure.Should().NotBeNull();
        dependecyStructure.MainOptionKey.Should().Be("DependencyInPageList:DependencyComponents:Parent");
    }

    [Fact]
    public void DependencyShouldBeCorrectInGroupInPageList()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(DependencyInPageList) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(2);
        page.Children[0].Should().BeOfType<PageList>();

        var pageList = page.Children[0] as PageList;
        pageList.Should().NotBeNull();
        pageList.PageEntries.Should().HaveCount(1);
        pageList.PageEntries[0].Should().NotBeNull();
        pageList.PageEntries[0].Should().BeOfType<Page>();

        var pageEntry = pageList.PageEntries[0] as Page;
        pageEntry.Should().NotBeNull();
        pageEntry.Children.Should().HaveCount(4);
        pageEntry.Children[1].Key.Should().Be("DependencyInPageList:DependencyComponents:0:Parent");

        pageEntry.Children[3].Should().NotBeNull();
        pageEntry.Children[3].Should().BeOfType<Group>();
        var group = pageEntry.Children[3] as Group;

        group.Should().NotBeNull();
        group.Children.Should().HaveCount(2);
        group.Children[1].Dependency.Should().NotBeEmpty();
        group.Children[1].Key.Should().Be("DependencyInPageList:DependencyComponents:0:GroupComponent:MainComponent");
        group.Children[1].Dependency.Should().HaveCount(1);
        var dependecy = group.Children[1].Dependency[0];
        dependecy.Should().NotBeNull();
        dependecy.MainOptionKey.Should().Be("DependencyInPageList:DependencyComponents:0:GroupComponent:Parent");

        pageList.PageStructure.Should().NotBeNull();
        var pageStructure = pageList.PageStructure as Page;
        pageStructure.Should().NotBeNull();
        pageStructure.Children[2].Dependency.Should().NotBeEmpty();
        pageStructure.Children[2].Key.Should().Be("DependencyInPageList:DependencyComponents:MainComponent");
        pageStructure.Children[2].Dependency.Should().HaveCount(1);

        pageStructure.Children[3].Should().NotBeNull();
        pageStructure.Children[3].Should().BeOfType<Group>();
        var groupPageStructure = pageStructure.Children[3] as Group;
        groupPageStructure.Should().NotBeNull();
        groupPageStructure.Children[1].Dependency.Should().HaveCount(1);
        var dependecyStructure = groupPageStructure.Children[1].Dependency[0];
        dependecyStructure.Should().NotBeNull();
        dependecyStructure.MainOptionKey.Should().Be("DependencyInPageList:DependencyComponents:GroupComponent:Parent");
    }

    [Fact]
    public void DependencyShouldBeCorrectInGroupDependsInPageList()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(DependencyInPageList) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(2);
        page.Children[1].Should().BeOfType<PageList>();

        var pageList = page.Children[1] as PageList;
        pageList.Should().NotBeNull();
        pageList.PageEntries.Should().HaveCount(1);
        pageList.PageEntries[0].Should().NotBeNull();
        pageList.PageEntries[0].Should().BeOfType<Page>();

        var pageEntry = pageList.PageEntries[0] as Page;
        pageEntry.Should().NotBeNull();
        pageEntry.Children.Should().HaveCount(3);
        pageEntry.Children[1].Key.Should().Be("DependencyInPageList:DependencyComponentsGroup:0:Parent");

        pageEntry.Children[2].Should().NotBeNull();
        pageEntry.Children[2].Should().BeOfType<Group>();
        var group = pageEntry.Children[2] as Group;

        group.Should().NotBeNull();
        group.Children.Should().HaveCount(2);
        group.Children[1].Dependency.Should().NotBeEmpty();
        group.Children[1].Key.Should().Be("DependencyInPageList:DependencyComponentsGroup:0:GroupComponent:MainComponent");
        group.Children[1].Dependency.Should().HaveCount(1);
        var dependecy = group.Children[1].Dependency[0];
        dependecy.Should().NotBeNull();
        dependecy.MainOptionKey.Should().Be("DependencyInPageList:DependencyComponentsGroup:0:GroupComponent:Parent");

        pageList.PageStructure.Should().NotBeNull();
        var pageStructure = pageList.PageStructure as Page;
        pageStructure.Should().NotBeNull();
        pageStructure.Children[2].Dependency.Should().NotBeEmpty();
        pageStructure.Children[2].Key.Should().Be("DependencyInPageList:DependencyComponentsGroup:GroupComponent");
        pageStructure.Children[2].Dependency.Should().HaveCount(1);

        pageStructure.Children[2].Should().NotBeNull();
        pageStructure.Children[2].Should().BeOfType<Group>();
        var groupPageStructure = pageStructure.Children[2] as Group;
        groupPageStructure.Should().NotBeNull();
        groupPageStructure.Children[1].Dependency.Should().HaveCount(1);
        var dependecyStructure1 = groupPageStructure.Children[1].Dependency[0];
        dependecyStructure1.Should().NotBeNull();
        dependecyStructure1.MainOptionKey.Should().Be("DependencyInPageList:DependencyComponentsGroup:GroupComponent:Parent");
    }

    [Fact]
    public void PageList_With_PageListMaxItemsAttribute_Should_Set_MaxItems()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithMaxItemsRoot) }).FirstOrDefault();
        var page = pages.Value;

        // Act
        var pageList = page.Children.OfType<PageList>().Single();

        // Assert
        pageList.MaxItems.Should().Be(3);

        // Optional extra safety checks: structure exists (so this isn't a false positive)
        pageList.PageStructure.Should().NotBeNull();
        pageList.PageEntries.Should().NotBeNull();
    }

    [Fact]
    public void PageList_With_MinItemsAttribute_Should_Set_MinItems()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithMinItemsRoot) }).FirstOrDefault();
        var page = pages.Value;

        // Act
        var pageList = page.Children.OfType<PageList>().Single();

        // Assert
        pageList.MinItems.Should().Be(1);
    }

    [Fact]
    public void PageList_With_PageListMaxItemsNotGreaterThanMinItems_Should_Throw()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithInvalidMaxItemsAndMinItemsRoot) }));

        exception.Message.Should().Contain("must have PageListMaxItems greater than PageListMinItems");
    }

    [Fact]
    public void PageList_Without_PageListMaxItemsAttribute_Should_Have_Null_MaxItems()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithoutMaxItemsRoot) }).FirstOrDefault();
        var page = pages.Value;

        // Act
        var pageList = page.Children.OfType<PageList>().Single();

        // Assert
        pageList.MaxItems.Should().BeNull();
    }

    [Fact]
    public void PageList_Without_PageListMinLimitsAttribute_Should_Have_Null_MinItems()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithoutMaxItemsRoot) }).FirstOrDefault();
        var page = pages.Value;

        // Act
        var pageList = page.Children.OfType<PageList>().Single();

        // Assert
        pageList.MinItems.Should().BeNull();
    }

    [Fact]
    public void PageList_DeepCopy_Should_Copy_MaxItems()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithMaxItemsRoot) }).FirstOrDefault();
        var page = pages.Value;
        var original = page.Children.OfType<PageList>().Single();

        // Act
        var copy = (PageList)original.DeepCopy("CopyRoot", "CopyRoot");

        // Assert
        copy.MaxItems.Should().Be(original.MaxItems);
    }

    [Fact]
    public void PageList_DeepCopy_Should_Copy_MinItems()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(PageListWithMinItemsRoot) }).FirstOrDefault();
        var page = pages.Value;
        var original = page.Children.OfType<PageList>().Single();

        // Act
        var copy = (PageList)original.DeepCopy("CopyRoot", "CopyRoot");

        // Assert
        copy.MinItems.Should().Be(original.MinItems);
    }

    [Fact]
    public void PageList_DeepCopy_Should_Copy_ExcludeFromImportExport()
    {
        // Arrange
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ExcludedImportExportPageListRoot) }).FirstOrDefault();
        var page = pages.Value;
        var original = page.Children.OfType<PageList>().Single();

        // Act
        var copy = (PageList)original.DeepCopy("CopyRoot", "CopyRoot");

        // Assert
        copy.ExcludeFromImportExport.Should().Be(original.ExcludeFromImportExport);
    }

    [Fact]
    public void ProviderManagedPageList_Should_Configure_Key_And_ReadOnly_Metadata()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ProviderManagedPageListRoot) }).FirstOrDefault();
        var page = pages.Value;

        var pageList = page.Children.OfType<PageList>().Single();
        var pageStructure = pageList.PageStructure.As<Page>();
        var keyOption = pageStructure.Children.OfType<Option>().Single(o => o.Key == "ProviderManagedPageListRoot:Entries:EntryKey");
        var nameOption = pageStructure.Children.OfType<Option>().Single(o => o.Key == "ProviderManagedPageListRoot:Entries:Name");

        pageList.IsProviderManaged.Should().BeTrue();
        pageList.ExcludeFromImportExport.Should().BeFalse();
        pageList.EntryProviderKey.Should().Be(nameof(TestPageListEntryProvider));
        pageList.ProviderKeyPropertyName.Should().Be(nameof(ProviderManagedEntry.EntryKey));
        keyOption.IsHidden.Should().BeTrue();
        nameOption.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void ProviderManagedPageList_Should_Not_Require_Name_Property()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ProviderManagedPageListWithoutNameRoot) }).FirstOrDefault();
        var page = pages.Value;

        var pageList = page.Children.OfType<PageList>().Single();
        var pageStructure = pageList.PageStructure.As<Page>();
        var keyOption = pageStructure.Children.OfType<Option>().Single(o => o.Key == "ProviderManagedPageListWithoutNameRoot:Entries:EntryKey");
        var nameOption = pageStructure.Children.OfType<Option>().Single(o => o.Key == "ProviderManagedPageListWithoutNameRoot:Entries:Name");

        pageList.IsProviderManaged.Should().BeTrue();
        pageList.ColumnHeaderLabels.Should().Equal(PageList.ProviderManagedDisplayNameColumnHeader, "ProviderManagedEnabled");
        nameOption.IsNameColumn.Should().BeTrue();
        nameOption.IsReadOnly.Should().BeTrue();
        nameOption.Value.Should().BeEmpty();
        keyOption.IsHidden.Should().BeTrue();
    }

    [Fact]
    public async Task ProviderManagedPageList_WithoutName_Should_ShowProviderDisplayName_OnEntryPage()
    {
        SettingsPagesProvider.Initialize([typeof(InstrumentSettings).Assembly], _ => [typeof(InstrumentSettings)]);

        var settings = new List<Setting>
        {
            new() { Key = "Instrument:Instruments:0:SerialNumber", Value = "SN-1" }
        };
        var dbContext = new Mock<ISettingsDbContext>();
        dbContext.Setup(x => x.Settings).ReturnsDbSet(settings);

        var entryProvider = new Mock<IPageListEntryProvider>();
        entryProvider
            .Setup(x => x.GetEntriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PageListEntry
                {
                    Key = "SN-1",
                    DisplayName = "Instrument A"
                }
            ]);
        var services = new ServiceCollection()
            .AddKeyedSingleton<IPageListEntryProvider>(InstrumentSettingsProviderKeys.InstrumentEntryProvider, entryProvider.Object)
            .BuildServiceProvider();
        var loader = new SettingsPagesLoader(dbContext.Object, _localizer, services);

        var pages = await loader.LoadAsync("Instrument", TestContext.Current.CancellationToken);

        var entryPage = pages["Instrument:Instruments:0"];
        var nameOption = entryPage.Children.OfType<Option>().Single(o => o.Key == "Instrument:Instruments:0:Name");

        entryPage.Label.Should().Be("Instrument A");
        nameOption.Value.Should().Be("Instrument A");
        nameOption.IsNameColumn.Should().BeTrue();
        nameOption.IsReadOnly.Should().BeTrue();
        nameOption.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task ProviderManagedPageList_WithoutPersistedSettings_Should_CreateEntryPageWithDefaultChildren()
    {
        SettingsPagesProvider.Initialize([typeof(InstrumentSettings).Assembly], _ => [typeof(InstrumentSettings)]);

        var dbContext = new Mock<ISettingsDbContext>();
        dbContext.Setup(x => x.Settings).ReturnsDbSet([]);

        var entryProvider = new Mock<IPageListEntryProvider>();
        entryProvider
            .Setup(x => x.GetEntriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new PageListEntry
                {
                    Key = "SN-NEW",
                    DisplayName = "New Instrument"
                }
            ]);
        var services = new ServiceCollection()
            .AddKeyedSingleton<IPageListEntryProvider>(InstrumentSettingsProviderKeys.InstrumentEntryProvider, entryProvider.Object)
            .BuildServiceProvider();
        var loader = new SettingsPagesLoader(dbContext.Object, _localizer, services);

        var pages = await loader.LoadAsync("Instrument", TestContext.Current.CancellationToken);

        var entryPage = pages["Instrument:Instruments:0"];
        var nameOption = entryPage.Children.OfType<Option>().Single(o => o.Key == "Instrument:Instruments:0:Name");

        entryPage.Label.Should().Be("New Instrument");
        nameOption.Value.Should().Be("New Instrument");
        nameOption.IsHidden.Should().BeFalse();
        entryPage.Children.OfType<Option>().Single(o => o.Key == "Instrument:Instruments:0:SerialNumber").Value.Should().Be("SN-NEW");
        entryPage.Children.OfType<Group>().Select(g => g.Key).Should().BeEquivalentTo(
        [
            "Instrument:Instruments:0:Reagents",
            "Instrument:Instruments:0:Waste",
            "Instrument:Instruments:0:Rinse",
            "Instrument:Instruments:0:DrawerLock"
        ]);

        var reagentsGroup = entryPage.Children.OfType<Group>().Single(g => g.Key == "Instrument:Instruments:0:Reagents");
        reagentsGroup.Children.OfType<Option>().Single(o => o.Key == "Instrument:Instruments:0:Reagents:KeepExpiredReagentsOnBoard").Value.Should().Be("False");
        reagentsGroup.Children.OfType<Option>().Single(o => o.Key == "Instrument:Instruments:0:Reagents:KeepEmptyReagentsOnBoard").Value.Should().Be("False");
    }

    [Fact]
    public void ExcludeFromImportExport_Should_Configure_PageList_Metadata()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ExcludedImportExportPageListRoot) }).FirstOrDefault();
        var page = pages.Value;

        var pageList = page.Children.OfType<PageList>().Single();

        pageList.IsProviderManaged.Should().BeFalse();
        pageList.ExcludeFromImportExport.Should().BeTrue();
    }

    [Fact]
    public void ExcludeFromImportExport_Should_Configure_Option_Property_And_Class_Metadata()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ExcludeFromImportExportRoot) }).FirstOrDefault();
        var page = pages.Value;

        var secretOption = page.Children.OfType<Option>().Single(o => o.Key == "ExcludeFromImportExportRoot:Secret");
        var visibleOption = page.Children.OfType<Option>().Single(o => o.Key == "ExcludeFromImportExportRoot:Visible");
        var excludedGroup = page.Children.OfType<Group>().Single(g => g.Key == "ExcludeFromImportExportRoot:ExcludedGroup");
        var excludedPage = page.Children.OfType<Page>().Single(p => p.Key == "ExcludeFromImportExportRoot:ExcludedPage");

        secretOption.ExcludeFromImportExport.Should().BeTrue();
        visibleOption.ExcludeFromImportExport.Should().BeFalse();
        excludedGroup.ExcludeFromImportExport.Should().BeTrue();
        excludedPage.ExcludeFromImportExport.Should().BeTrue();
    }

    [Fact]
    public void BaseDefaultSettingsProvider_Should_Build_ImportExport_Exclusions_For_Exact_Keys_And_Subtrees()
    {
        BaseDefaultSettingsProvider.Initialize(
            [GetType().Assembly],
            _ => [typeof(ExcludeFromImportExportRoot), typeof(ExcludedImportExportPageListRoot), typeof(ExcludedImportExportPageListNestedRoot), typeof(ProviderManagedPageListRoot)]);

        var exclusions = BaseDefaultSettingsProvider.GetImportExportExclusions();

        exclusions.IsExcluded("ExcludeFromImportExportRoot:Secret").Should().BeTrue();
        exclusions.IsExcluded("ExcludeFromImportExportRoot:Visible").Should().BeFalse();
        exclusions.IsExcluded("ExcludeFromImportExportRoot:ExcludedGroup:Child").Should().BeTrue();
        exclusions.IsExcluded("ExcludeFromImportExportRoot:ExcludedPage:Child").Should().BeTrue();
        exclusions.IsExcluded("ExcludedImportExportPageListRoot:Entries:0:Name").Should().BeTrue();
        exclusions.IsExcluded("ExcludedImportExportPageListNestedRoot:Entries:0:Details:Child").Should().BeTrue();
        exclusions.IsExcluded("ProviderManagedPageListRoot:Entries:0:Name").Should().BeFalse();
    }

    [Fact]
    public void ProviderManagedPageList_Should_Require_Configured_Key_Property()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ProviderManagedPageListMissingKeyRoot) }));

        exception.Message.Should().Contain("must have key property 'MissingKey'");
    }

    [Fact]
    public void ProviderManagedPageList_Should_Require_String_Key_Property()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ProviderManagedPageListNonStringKeyRoot) }));

        exception.Message.Should().Contain("must be a read/write string property");
    }

    [Fact]
    public void ObfuscateSettingAttribute_ShouldSetIsObfuscatedOnOption()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ObfuscateSettingPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(2);

        var obfuscatedOption = page.Children[0].As<Option>();
        obfuscatedOption.Key.Should().Be("ObfuscateSettingPage:Password");
        obfuscatedOption.IsObfuscated.Should().BeTrue();

        var normalOption = page.Children[1].As<Option>();
        normalOption.Key.Should().Be("ObfuscateSettingPage:NormalString");
        normalOption.IsObfuscated.Should().BeFalse();
    }

    [Fact]
    public void ProtectedDataAttribute_ShouldSetIsEncryptedOnOption()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ProtectedDataPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(1);
        page.Children[0].As<Option>().IsEncrypted.Should().BeTrue();
        page.Children[0].As<Option>().IsObfuscated.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(InvalidDefaultInt))]
    [InlineData(typeof(InvalidDefaultInt2))]
    [InlineData(typeof(InvalidDefaultDouble))]
    [InlineData(typeof(InvalidDefaultDouble2))]
    [InlineData(typeof(InvalidDefaultString))]
    public void InvalidDefaultValueShouldThrowValidationException(Type settingsType)
    {
        var exception = Assert.Throws<ValidationException>(() =>
        {
            var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { settingsType });
        });
    }

    [Fact]
    public void InvalidDefaultValue_InPageListStructure_ShouldNotThrowValidationException()
    {
        // PageList structure is built from Activator.CreateInstance(elementType).
        // Default values inside PageList entry types are not validated during metadata creation.
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(InvalidDefaultInPageList) });
    }

    [Fact]
    public void DefualtValueAttributeShouldNotBeUSed()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(DefaultValueAttributeNotUsed) }).FirstOrDefault(); ;
        var page = pages.Value;

        page.Children.Should().HaveCount(1);
        page.Children[0].Should().BeOfType<Option>();
        page.Children[0].As<Option>().Label.Should().Be("Default");
        page.Children[0].As<Option>().DefaultValue.Should().Be("0");

    }

    [Fact]
    public void SettingsAction_CreatesActionButton_NotOption()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ActionButtonPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(1);
        page.Children[0].Should().BeOfType<ActionButton>();
        page.Children[0].As<ActionButton>().ActionKey.Should().Be(ActionKey.Export);
        page.Children[0].As<ActionButton>().Key.Should().Be("ActionButtonPage:ExportButton");
        page.Children[0].As<ActionButton>().Label.Should().Be("ActionButton_Export_Label");
    }

    [Fact]
    public void SettingsAction_LabelHelpText_IsReadFromProperty()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ActionButtonPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children[0].Should().BeOfType<ActionButton>();
        page.Children[0].As<ActionButton>().LabelHelpText.Should().Be("ActionButton_Export_HelpText");
    }

    [Fact]
    public void SettingsAction_ImportAction_IsMappedCorrectly()
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer, _ => new[] { typeof(ActionButtonImportPage) }).FirstOrDefault();
        var page = pages.Value;

        page.Children.Should().HaveCount(1);
        page.Children[0].Should().BeOfType<ActionButton>();
        page.Children[0].As<ActionButton>().ActionKey.Should().Be(ActionKey.Import);
        page.Children[0].As<ActionButton>().Key.Should().Be("ActionButtonImportPage:ImportButton");
        page.Children[0].As<ActionButton>().Label.Should().Be("ActionButton_Import_Label");
        page.Children[0].As<ActionButton>().LabelHelpText.Should().Be("ActionButton_Import_HelpText");
    }

    [Fact]
    public void SettingsAction_IsNotDefaultSettingOrOption()
    {
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ActionButtonWithOptionPage)]);

        var defaultSettings = BaseDefaultSettingsProvider.GetDefaultSettingsAndPageStructure();
        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();

        defaultSettings.Should().ContainKey("ActionButtonWithOptionPage:RegularBoolean");
        defaultSettings.Should().NotContainKey("ActionButtonWithOptionPage:ExportButton");
        defaultOptions.Should().ContainKey("ActionButtonWithOptionPage:RegularBoolean");
        defaultOptions.Should().NotContainKey("ActionButtonWithOptionPage:ExportButton");
    }

    [Fact]
    public void SettingsAction_IsExcludedFromImportExport()
    {
        BaseDefaultSettingsProvider.Initialize([GetType().Assembly], _ => [typeof(ActionButtonPage)]);

        var exclusions = BaseDefaultSettingsProvider.GetImportExportExclusions();

        exclusions.IsExcluded("ActionButtonPage:ExportButton").Should().BeTrue();
    }

    [Trait("ProductRequirement", "PR_530")]
    [Trait("ModuleRequirement", "MR_6000")]
    [Fact]
    public void LabCommunitySettings_Username_And_Password_AreExcludedFromImportExport()
    {
        BaseDefaultSettingsProvider.Initialize(
            [typeof(LabCommunitySettings).Assembly],
            _ => [typeof(LabCommunitySettings)]);

        var exclusions = BaseDefaultSettingsProvider.GetImportExportExclusions();

        exclusions.IsExcluded("LabCommunity:Username").Should().BeTrue();
        exclusions.IsExcluded("LabCommunity:Password").Should().BeTrue();
    }
}

[RootConfigSection(sortOrder: 1)]
internal class MissingRootDisplayName
{
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class MissingSettingDisplayName
{
    public string? StringSetting { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class MissingPageDisplayName
{
    public BasicPage? Page { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class MissingGroupDisplayName
{
    [SettingsGroup]
    public BasicPage? Group { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class MissingPageListDisplayName
{
    public IEnumerable<ComplexComponent> ComplexComponents { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class BasicPage
{
    [DisplayNameLocal("String Label")]
    public string? StringSetting { get; set; }
    [DisplayNameLocal("Int Label")]
    public int IntSetting { get; set; }
    [DisplayNameLocal("PageOrientation")]
    public PageOrientation PageOrientation { get; set; } = PageOrientation.Portrait;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class CorrectDependencyPage
{
    [DisplayNameLocal("Depend")]
    public bool Depend { get; set; }
    [DependsFrom([nameof(Depend)], ["false"], DependencyType.Hide)]
    [DisplayNameLocal("Int Label")]
    public int IntSetting { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
[NonOverlappingRanges(
    nameof(FirstStart),
    nameof(FirstLength),
    nameof(SecondStart),
    nameof(SecondLength),
    [nameof(Enabled)],
    ["True"])]
internal class NonOverlappingRangesPage
{
    [DisplayNameLocal("Enabled")]
    public bool Enabled { get; set; }

    [DisplayNameLocal("FirstStart")]
    public int FirstStart { get; set; } = 1;

    [DisplayNameLocal("FirstLength")]
    public int FirstLength { get; set; } = 1;

    [DisplayNameLocal("SecondStart")]
    public int SecondStart { get; set; } = 2;

    [DisplayNameLocal("SecondLength")]
    public int SecondLength { get; set; } = 1;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class CorrectGroupDependencyPage
{
    [DisplayNameLocal("Depend")]
    public bool Depend { get; set; }
    [DependsFrom([nameof(Depend)], ["false"], DependencyType.Hide)]
    [SettingsGroup]
    [DisplayNameLocal("Group")]
    public BasicPage? Group { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class CorrectPageDependencyPage
{
    [DisplayNameLocal("String Label")]
    public bool Depend { get; set; }
    [DependsFrom([nameof(Depend)], ["false"], DependencyType.Hide)]
    [DisplayNameLocal("Page")]
    public BasicPage? Page { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class CorrectMultipleDependencyPage
{
    [DisplayNameLocal("Depend")]
    public bool Depend { get; set; }

    [DisplayNameLocal("Depend2")]
    public string Depend2 { get; set; } = "";

    [DependsFrom([nameof(Depend), nameof(Depend2)], ["false", "full"], DependencyType.Disable)]
    [DisplayNameLocal("Int Label")]
    public int IntSetting { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class IncorrectMultipleDependencyValuePage
{
    [DisplayNameLocal("Depend")]
    public bool Depend { get; set; }

    [DisplayNameLocal("Depend2")]
    public string Depend2 { get; set; } = "";

    [DependsFrom([nameof(Depend), nameof(Depend2)], ["false"], DependencyType.Disable)]
    [DisplayNameLocal("Int Label")]
    public int IntSetting { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class IncorrectMultipleDependencyPropertyPage
{
    [DisplayNameLocal("Depend")]
    public bool Depend { get; set; }

    [DisplayNameLocal("Depend2")]
    public string Depend2 { get; set; } = "";

    [DependsFrom([nameof(Depend)], ["false", "ok"], DependencyType.Disable)]
    [DisplayNameLocal("Int Label")]
    public int IntSetting { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class IncorrectDependencyPage
{
    [DisplayNameLocal("String Label")]
    public string? StringSetting { get; set; }
    [DisplayNameLocal("IncorrectdDependencyPage")]
    public IncorrectdDependencyPage? IncorrectdDependencyPage { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class IncorrectDependencyOrderPage
{
    [DisplayNameLocal("String Label")]
    [DependsFrom([nameof(DependingAfter)], ["False"], DependencyType.Hide)]
    public string? StringSetting { get; set; }

    [DisplayNameLocal("DependingAfter")]
    public bool DependingAfter { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class IncorrectDependencyDefinitionPage
{
    [DisplayNameLocal("String Label")]
    [DependsFrom([], [], DependencyType.Hide)]
    public string? StringSetting { get; set; }

    [DisplayNameLocal("DependingAfter")]
    public bool DependingAfter { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class GroupPage
{
    [SettingsGroup]
    [DisplayNameLocal("GroupOne Label")]
    public BasicComponent? GroupOne { get; set; }

    [SettingsGroup]
    [DisplayNameLocal("GroupTwo Label")]
    public BasicComponent GroupTwo { get; set; } = new BasicComponent
    {
        BasicString = "Initialized Group"
    };
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ComplexPageListPage
{
    [DisplayNameLocal("ComplexComponents")]
    public IEnumerable<ComplexComponent> ComplexComponents { get; set; } = new List<ComplexComponent> {
        new() {
            Name = "Initialized Name",
            MainComponent = "Initialized Value",
            SubPage = new BasicPageComponent
            {
                BasicString = "SubPage Initialized Value",
                SubGroup1 =  new BasicComponent
            {
                BasicString = "SubPageGroup Initialized Value"
            },
            },
            SubGroup = new BasicComponent
            {
                BasicString = "SubGroup Initialized Value"
            },
        },
        new(), // Using default values
    };
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("TopPage Label")]
[GroupHeader(typeof(string), "Header")]
internal class PageWithSubPage
{
    [DisplayNameLocal("SubPage Label")]
    public BasicPageComponent? SubPageOne { get; set; }

    [DisplayNameLocal("SubPageTwo Label")]
    public BasicPageComponent? SubPageTwo { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class InvalidDefaultInt
{
    [DisplayNameLocal("InvalidInt")]
    [SettingsRange(0, 10)]
    public int InvalidInt { get; set; } = -1;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class InvalidDefaultInt2
{
    [DisplayNameLocal("InvalidInt")]
    [SettingsRange(0, 10)]
    public int InvalidInt { get; set; } = 11;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class InvalidDefaultDouble
{
    [DisplayNameLocal("InvalidDouble")]
    [SettingsRange(0.0, 10.0)]
    public double InvalidDouble { get; set; } = -1.0;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class InvalidDefaultDouble2
{
    [DisplayNameLocal("InvalidDouble")]
    [SettingsRange(0.0, 10.0)]
    public double InvalidDouble { get; set; } = 11.0;
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class InvalidDefaultString
{
    [DisplayNameLocal("InvalidString")]
    [MaxLength(5)]
    public string InvalidString { get; set; } = "Overthelimit";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class DefaultValueAttributeNotUsed
{
    [DisplayNameLocal("Default")]
    [DefaultValue(10)]
    public int InvalidString { get; set; }
}


[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class InvalidDefaultInPageList
{
    [DisplayNameLocal("DefaultValuePageList")]
    public IEnumerable<DefaultValuePageList> DefaultValuePageList { get; set; } = [];
}

[DisplayNameLocal("DefaultValuePageList")]
internal class DefaultValuePageList
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "Default Name";

    [DisplayNameLocal("DefaultValuePageListInPagelist")]
    public IEnumerable<DefaultValuePageListInPagelist> DefaultValuePageListInPagelist { get; set; } = [];
}

[DisplayNameLocal("DefaultValuePageListInPagelist")]
internal class DefaultValuePageListInPagelist
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "Default Name";

    [DisplayNameLocal("InvalidInt")]
    [SettingsRange(0, 10)]
    public int InvalidInt { get; set; } = 11;
}


[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class IncorrectDynamicDropdownPage
{
    [DynamicDropdown(nameof(ComplexComponents), nameof(ComplexComponent.MainComponent))]
    [DisplayNameLocal("DynamicDropdownProperty")]
    public string? DynamicDropdownProperty { get; set; }

    [DisplayNameLocal("ComplexComponents")]
    public IEnumerable<ComplexComponent> ComplexComponents { get; set; } = new List<ComplexComponent> {
        new() {
            Name = "Initialized Name",
            MainComponent = "Initialized Value",
        },
    };
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class DynamicDropdownPage
{
    [DynamicDropdown(nameof(ComplexComponents), nameof(ComplexComponent.MainComponent))]
    [DisplayNameLocal("DynamicDropdown_Test_Label")]
    public string DynamicDropdownProperty { get; set; } = string.Empty;

    [DisplayNameLocal("ComplexComponents")]
    public IEnumerable<ComplexComponent> ComplexComponents { get; set; } = new List<ComplexComponent> {
    new() {
        Name = "Initialized Name",
        MainComponent = "Initialized Value",
    },
    };
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class DependencyInPageList
{

    [DisplayNameLocal("DependencyComponents")]
    public IEnumerable<DependencyComponent> DependencyComponents { get; set; } = new List<DependencyComponent> {
    new() {
        Name = "DC",
        Parent = false,
        MainComponent = "Initialized Value",
    },
    };

    [DisplayNameLocal("DependencyComponents")]
    public IEnumerable<DependencyComponentGroupDepends> DependencyComponentsGroup { get; set; } = new List<DependencyComponentGroupDepends> {
    new() {
        Name = "DC",
        Parent = false,
    },
    };
}

internal class ComplexComponent
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";

    [DisplayNameLocal("MainComponent")]
    public string MainComponent { get; set; } = "Default Main Component Value";

    [DisplayNameLocal("SubPage")]
    public BasicPageComponent? SubPage { get; set; }

    [SettingsGroup]
    [DisplayNameLocal("SubGroup")]
    public BasicComponent? SubGroup { get; set; }
}

internal class DependencyComponent
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";

    [DisplayNameLocal("Parent")]
    public bool Parent { get; set; }

    [DisplayNameLocal("MainComponent")]
    [DependsFrom(nameof(Parent), "True", DependencyType.Hide)]
    public string MainComponent { get; set; } = "Default Main Component Value";

    [SettingsGroup]
    [DisplayNameLocal("GroupComponent")]
    public DependencyGroupComponent GroupComponent { get; set; } = new DependencyGroupComponent()
    {
        Parent = false,
        MainComponent = "Default"
    };
}

internal class DependencyComponentGroupDepends
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";

    [DisplayNameLocal("Parent")]
    public bool Parent { get; set; }

    [SettingsGroup]
    [DisplayNameLocal("GroupComponent")]
    [DependsFrom(nameof(Parent), "True", DependencyType.Hide)]
    public DependencyGroupComponent GroupComponent { get; set; } = new DependencyGroupComponent()
    {
        Parent = false,
        MainComponent = "Default"
    };
}

internal class DependencyGroupComponent
{

    [DisplayNameLocal("GroupParent")]
    public bool Parent { get; set; }

    [DisplayNameLocal("GroupMainComponent")]
    [DependsFrom(nameof(Parent), "True", DependencyType.Hide)]
    public string MainComponent { get; set; } = "Default Main Component Value";
}

internal class BasicComponent
{
    [DisplayNameLocal("BasicString")]
    public string BasicString { get; set; } = "Default String Value";
}

internal class BasicPageComponent
{
    [DisplayNameLocal("BasicString")]
    public string BasicString { get; set; } = "Default String Value";

    [SettingsGroup]
    [DisplayNameLocal("SubGroup1")]
    public BasicComponent? SubGroup1 { get; set; }
}

internal class IncorrectdDependencyPage
{
    [DependsFrom([nameof(IncorrectDependencyPage.StringSetting)], ["false"], DependencyType.Hide)]
    [DisplayNameLocal("IncorrectDependency")]
    public bool IncorrectDependency { get; set; }
}

internal class IncorrectTable
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";

    [DisplayNameLocal("Correct")]
    public string? Correct { get; set; }

    [DisplayNameLocal("Incorrect")]
    public MethodReportComment? Incorrect { get; set; }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class PageListWithMaxItemsRoot
{
    [DisplayNameLocal("Components")]
    [PageListMaxItems(3)]
    public IEnumerable<PageListMaxItemsComponent> Components { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class PageListWithMinItemsRoot
{
    [DisplayNameLocal("Components")]
    [PageListMinItems(1)]
    public IEnumerable<PageListMaxItemsComponent> Components { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class PageListWithInvalidMaxItemsAndMinItemsRoot
{
    [DisplayNameLocal("Components")]
    [PageListMaxItems(1)]
    [PageListMinItems(1)]
    public IEnumerable<PageListMaxItemsComponent> Components { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class PageListWithoutMaxItemsRoot
{
    [DisplayNameLocal("Components")]
    public IEnumerable<PageListMaxItemsComponent> Components { get; set; } = [];
}

internal class PageListMaxItemsComponent
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class NestedTableColumnPageListRoot
{
    [DisplayNameLocal("Entries")]
    public IEnumerable<NestedTableColumnPageListEntry> Entries { get; set; } =
    [
        new()
        {
            Name = "Entry",
            Details = new NestedTableColumnDetails
            {
                NestedColumn = "Nested value"
            }
        }
    ];
}

internal class NestedTableColumnPageListEntry
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";

    [SettingsGroup]
    [DisplayNameLocal("Details")]
    public NestedTableColumnDetails Details { get; set; } = new();
}

internal class NestedTableColumnDetails
{
    [DisplayNameLocal("NestedColumn")]
    [TableColumn]
    public string NestedColumn { get; set; } = "";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ProviderManagedPageListRoot
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(TestPageListEntryProvider), nameof(ProviderManagedEntry.EntryKey))]
    public IEnumerable<ProviderManagedEntry> Entries { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ExcludedImportExportPageListRoot
{
    [DisplayNameLocal("Entries")]
    [ExcludeFromImportExport]
    public IEnumerable<PageListMaxItemsComponent> Entries { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ExcludedImportExportPageListNestedRoot
{
    [DisplayNameLocal("Entries")]
    public IEnumerable<ExcludedImportExportPageListNestedEntry> Entries { get; set; } = [];
}

internal class ExcludedImportExportPageListNestedEntry
{
    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";

    [DisplayNameLocal("Details")]
    [ExcludeFromImportExport]
    public ExcludeFromImportExportComponent Details { get; set; } = new();
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ExcludeFromImportExportRoot
{
    [DisplayNameLocal("Secret")]
    [ExcludeFromImportExport]
    public string Secret { get; set; } = "";

    [DisplayNameLocal("Visible")]
    public string Visible { get; set; } = "";

    [SettingsGroup]
    [DisplayNameLocal("ExcludedGroup")]
    [ExcludeFromImportExport]
    public ExcludeFromImportExportComponent ExcludedGroup { get; set; } = new();

    [DisplayNameLocal("ExcludedPage")]
    public ExcludeFromImportExportPage ExcludedPage { get; set; } = new();
}

internal class ExcludeFromImportExportComponent
{
    [DisplayNameLocal("Child")]
    public string Child { get; set; } = "";
}

[ExcludeFromImportExport]
internal class ExcludeFromImportExportPage
{
    [DisplayNameLocal("Child")]
    public string Child { get; set; } = "";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ProviderManagedPageListMissingKeyRoot
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(TestPageListEntryProvider), "MissingKey")]
    public IEnumerable<ProviderManagedEntry> Entries { get; set; } = [];
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ProviderManagedPageListNonStringKeyRoot
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(TestPageListEntryProvider), nameof(ProviderManagedEntryWithNonStringKey.EntryKey))]
    public IEnumerable<ProviderManagedEntryWithNonStringKey> Entries { get; set; } = [];
}

internal class ProviderManagedEntry
{
    public string EntryKey { get; set; } = "";

    [DisplayNameLocal("Name")]
    [ReadOnlySetting]
    public string Name { get; set; } = "";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ProviderManagedPageListWithoutNameRoot
{
    [DisplayNameLocal("Entries")]
    [ProviderPageList(nameof(TestPageListEntryProvider), nameof(ProviderManagedEntryWithoutName.EntryKey))]
    public IEnumerable<ProviderManagedEntryWithoutName> Entries { get; set; } = [];
}

internal class ProviderManagedEntryWithoutName
{
    public string EntryKey { get; set; } = "";

    [DisplayNameLocal("ProviderManagedEnabled")]
    [TableColumn]
    public bool Enabled { get; set; }
}

internal class ProviderManagedEntryWithNonStringKey
{
    public int EntryKey { get; set; }

    [DisplayNameLocal("Name")]
    public string Name { get; set; } = "";
}

internal sealed class TestPageListEntryProvider : IPageListEntryProvider
{
    public Task<IReadOnlyList<PageListEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PageListEntry>>([]);
    }
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ObfuscateSettingPage
{
    [ObfuscateSetting]
    [DisplayNameLocal("Password")]
    public string Password { get; set; } = "";

    [DisplayNameLocal("NormalString")]
    public string NormalString { get; set; } = "";
}

[RootConfigSection(sortOrder: 1)]
[DisplayNameLocal("RootLabel")]
internal class ProtectedDataPage
{
    [ProtectedData]
    [DisplayNameLocal("Token")]
    public string Token { get; set; } = "";
}

[RootConfigSection(sortOrder: 99)]
[DisplayNameLocal("ActionButtonPage")]
internal class ActionButtonPage
{
    [DisplayNameLocal("ActionButton_Export_Label")]
    [HelpText(typeof(string), "ActionButton_Export_HelpText")]
    public SettingsAction ExportButton { get; set; } = new(ActionKey.Export);
}

[RootConfigSection(sortOrder: 99)]
[DisplayNameLocal("ActionButtonImportPage")]
internal class ActionButtonImportPage
{
    [DisplayNameLocal("ActionButton_Import_Label")]
    [HelpText(typeof(string), "ActionButton_Import_HelpText")]
    public SettingsAction ImportButton { get; set; } = new(ActionKey.Import);
}

[RootConfigSection(sortOrder: 99)]
[DisplayNameLocal("ActionButtonWithOptionPage")]
internal class ActionButtonWithOptionPage
{
    [DisplayNameLocal("RegularBoolean")]
    public bool RegularBoolean { get; set; } = true;

    [DisplayNameLocal("ActionButton_Export_Label")]
    public SettingsAction ExportButton { get; set; } = new(ActionKey.Export);
}
