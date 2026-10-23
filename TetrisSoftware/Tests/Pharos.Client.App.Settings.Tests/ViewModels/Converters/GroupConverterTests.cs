using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels.Converters
{
    public class GroupConverterTests
    {
        private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer;
        private readonly Mock<IConverter> _mockConverter;
        private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService;
        private readonly GroupConverter _converter;
        private readonly PageConverter _pageConverter;
        private readonly OptionConverter _optionConverter;
        private readonly PageListConverter _pageListConverter;

        public GroupConverterTests()
        {
            _mockLocalizer = LocalizerMockFactory.Create();
            _mockConverter = new Mock<IConverter>();
            _mockDataService = new Mock<ISettingsDataService<SettingItemBase>>();
            _converter = new GroupConverter(_mockLocalizer.Object);
            _pageConverter = new PageConverter(_mockLocalizer.Object);
            _optionConverter = new OptionConverter(_mockLocalizer.Object);
            _pageListConverter = new PageListConverter(_mockLocalizer.Object);
        }

        [Fact]
        public void Type_ShouldReturnPageType()
        {
            _converter.Type.Should().Be<Group>();
        }

        [Fact]
        public void TryConvert_InvalidItem_ReturnsFalse()
        {
            var result = _converter.TryConvert(new SettingItemBase(), _mockConverter.Object, _mockDataService.Object, out var viewModel);

            result.Should().BeFalse();
            viewModel.Should().BeNull();
        }

        [Fact]
        public void PopulateDynamicDropdowns_WithValidData_PopulatesChoicesCorrectly()
        {
            // Arrange
            var group = new Group
            {
                Key = "parent",
                Label = "Parent Group",
                Children = new List<SettingsChildItem>(),
                Dependency = []
            };

            var dynamicDropdown = new Option
            {
                Key = "parent:dropdown1",
                DynamicDropdownOrigin = "targetList",
                DynamicDropdownValueTarget = "targetValue",
                Type = OptionType.Choice,
                Choices = [],
                Dependency = []
            };

            var targetOption = new Option
            {
                Key = "parent:targetList:0:entry1:targetValue",
                Label = "Target Option",
                Type = OptionType.String,
                Value = "value1",
                Dependency = []
            };
            group.Children.Add(dynamicDropdown);

            var targetPage = new Page
            {
                Key = "parent:targetList:0:entry1",
                Label = "Entry 1",
                Children = new List<SettingsChildItem>
                {
                    targetOption
                },
                Dependency = []
            };

            var pageStructure = new PagePlaceholder
            {
                Label = "Page Structure"
            };

            var targetPageList = new PageList
            {
                Key = "parent:targetList",
                PageEntries = new List<ChildPageItem>
                {
                    targetPage
                },
                PageStructure = pageStructure,
                ColumnHeaderLabels = [],
                Columns = [],
                DynamicDropdowns = [],
                Dependency = []
            };
            group.Children.Add(targetPageList);

            _optionConverter.TryConvert(targetOption, _mockConverter.Object, _mockDataService.Object, out var targetOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:targetList:0:entry1:targetValue")))
                .Returns(targetOptionViewModel!);

            _pageConverter.TryConvert(targetPage, _mockConverter.Object, _mockDataService.Object, out var targetPageViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:targetList:0:entry1")))
                .Returns(targetPageViewModel!);

            _pageListConverter.TryConvert(targetPageList, _mockConverter.Object, _mockDataService.Object, out var targetPageListViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:targetList")))
                .Returns(targetPageListViewModel!);

            _optionConverter.TryConvert(dynamicDropdown, _mockConverter.Object, _mockDataService.Object, out var optionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:dropdown1")))
                .Returns(optionViewModel!);

            // Act
            _converter.TryConvert(group, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            viewModel.Should().NotBeNull();
            var groupViewModel = viewModel.Should().BeOfType<GroupViewModel>().Subject;
            groupViewModel.Items.Should().NotBeNull();
            groupViewModel.Items.Should().HaveCount(2);
            var dynamicDropdownViewModel = groupViewModel.Items.First().Should().BeOfType<ChoicesViewModel>().Subject;
            var dynamicDropdownModel = dynamicDropdownViewModel.Model.Should().BeOfType<Option>().Subject;
            dynamicDropdownModel.Choices.Should().NotBeNull();
            dynamicDropdownModel.Choices.Should().HaveCount(1);
            dynamicDropdownModel.Choices!.First().Value.Should().Be("value1");
            dynamicDropdownModel.Choices!.First().Label.Should().Be("Entry 1");
        }
    }
}