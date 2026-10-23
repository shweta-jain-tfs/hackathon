using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels.Converters
{
    public class PageConverterTests
    {
        private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer;
        private readonly Mock<IConverter> _mockConverter;
        private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService;
        private readonly PageConverter _converter;
        private readonly OptionConverter _optionConverter;
        private readonly PageListConverter _pageListConverter;
        private readonly GroupConverter _groupConverter;

        public PageConverterTests()
        {
            _mockLocalizer = LocalizerMockFactory.Create();
            _mockConverter = new Mock<IConverter>();
            _mockDataService = new Mock<ISettingsDataService<SettingItemBase>>();
            _converter = new PageConverter(_mockLocalizer.Object);
            _optionConverter = new OptionConverter(_mockLocalizer.Object);
            _pageListConverter = new PageListConverter(_mockLocalizer.Object);
            _groupConverter = new GroupConverter(_mockLocalizer.Object);
        }

        [Fact]
        public void Type_ShouldReturnPageType()
        {
            _converter.Type.Should().Be<Page>();
        }

        [Fact]
        public void TryConvert_InvalidItem_ReturnsFalse()
        {
            var result = _converter.TryConvert(new SettingItemBase(), _mockConverter.Object, _mockDataService.Object, out var viewModel);

            result.Should().BeFalse();
            viewModel.Should().BeNull();
        }

        [Fact]
        public void TryConvert_ValidItem_ReturnsTrueAndCreatesViewModel()
        {
            // Arrange
            var page = new Page
            {
                Label = "testLabel",
                Children = new List<SettingsChildItem>
               {
                   new Group { Key = "group:key", Label = "Group" }
               }
            };

            var groupViewModel = new GroupViewModel(_mockLocalizer.Object);

            _mockConverter.Setup(c => c.Convert(It.IsAny<SettingItemBase>())).Returns(groupViewModel);
            _mockLocalizer.Setup(m => m[page.Label]).Returns(new LocalizedString("localized!", "localized!"));

            // Act
            var result = _converter.TryConvert(page, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            result.Should().BeTrue();
            viewModel.Should().NotBeNull();
            var pageViewModel = viewModel.Should().BeOfType<PageViewModel>().Subject;
            pageViewModel.Model.Should().Be(page);
            pageViewModel.Items.Should().ContainSingle().Which.Should().Be(groupViewModel);
            pageViewModel.Label.Should().Be("testLabel");
        }

        [Fact]
        public void PopulateDynamicDropdowns_WithValidData_PopulatesChoicesCorrectly()
        {
            // Arrange
            var page = new Page
            {
                Key = "parent",
                Label = "Parent Page",
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
            page.Children.Add(dynamicDropdown);

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
                Label = "Page Structure",
                Dependency = []
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
            page.Children.Add(targetPageList);

            _optionConverter.TryConvert(targetOption, _mockConverter.Object, _mockDataService.Object, out var targetOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:targetList:0:entry1:targetValue")))
                .Returns(targetOptionViewModel!);

            _converter.TryConvert(targetPage, _mockConverter.Object, _mockDataService.Object, out var targetPageViewModel);
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
            _converter.TryConvert(page, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            viewModel.Should().NotBeNull();
            var pageViewModel = viewModel.Should().BeOfType<PageViewModel>().Subject;
            pageViewModel.Items.Should().NotBeNull();
            pageViewModel.Items.Should().HaveCount(2);
            var dynamicDropdownViewModel = pageViewModel.Items.First().Should().BeOfType<ChoicesViewModel>().Subject;
            var dynamicDropdownModel = dynamicDropdownViewModel.Model.Should().BeOfType<Option>().Subject;
            dynamicDropdownModel.Choices.Should().NotBeNull();
            dynamicDropdownModel.Choices.Should().HaveCount(1);
            dynamicDropdownModel.Choices!.First().Value.Should().Be("value1");
            dynamicDropdownModel.Choices!.First().Label.Should().Be("Entry 1");
        }

        [Fact]
        public void SetDependencies_WithSingleDependency_SetsDependencyCorrectly()
        {
            // Arrange
            var page = new Page
            {
                Key = "parent",
                Label = "Parent Page",
                Children = new List<SettingsChildItem>(),
                Dependency = []
            };

            var mainOption = new Option
            {
                Key = "parent:mainOption",
                Label = "Main Option",
                Type = OptionType.Boolean,
                Value = "True",
                Dependency = []
            };

            var dependentOption = new Option
            {
                Key = "parent:dependentOption",
                Label = "Dependent Option",
                Type = OptionType.String,
                Value = "test",
                Dependency = new List<Dependency>
        {
            new() {
                MainOptionKey = "parent:mainOption",
                MainOptionValue = "True"
            }
        }
            };

            page.Children.Add(mainOption);
            page.Children.Add(dependentOption);

            _optionConverter.TryConvert(mainOption, _mockConverter.Object, _mockDataService.Object, out var mainOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:mainOption")))
                .Returns(mainOptionViewModel!);

            _optionConverter.TryConvert(dependentOption, _mockConverter.Object, _mockDataService.Object, out var dependentOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:dependentOption")))
                .Returns(dependentOptionViewModel!);

            // Act
            _converter.TryConvert(page, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            viewModel.Should().NotBeNull();
            var pageViewModel = viewModel.Should().BeOfType<PageViewModel>().Subject;
            pageViewModel.Items.Should().NotBeNull();
            pageViewModel.Items.Should().HaveCount(2);

            var dependentViewModel = pageViewModel.Items[1].Should().BeOfType<StringViewModel>().Subject;
            dependentViewModel.DependencyViewModel.Should().NotBeNull();
            dependentViewModel.DependencyViewModel.Should().HaveCount(1);
            dependentViewModel.DependencyViewModel[0].Should().BeSameAs(pageViewModel.Items[0]);
        }

        [Fact]
        public void SetDependencies_WithMultipleDependencies_SetsDependenciesCorrectly()
        {
            // Arrange
            var page = new Page
            {
                Key = "parent",
                Label = "Parent Page",
                Children = new List<SettingsChildItem>(),
                Dependency = []
            };

            var firstOption = new Option
            {
                Key = "parent:firstOption",
                Label = "First Option",
                Type = OptionType.Boolean,
                Value = "True",
                Dependency = []
            };

            var secondOption = new Option
            {
                Key = "parent:secondOption",
                Label = "Second Option",
                Type = OptionType.Integer,
                MaxValue = "100",
                MinValue = "0",
                Value = "42",
                Dependency = []
            };

            var dependentOption = new Option
            {
                Key = "parent:dependentOption",
                Label = "Dependent Option",
                Type = OptionType.String,
                Value = "test",
                Dependency = new List<Dependency>
            {
                new() {
                    MainOptionKey = "parent:firstOption",
                    MainOptionValue = "True"
                },
                new() {
                    MainOptionKey = "parent:secondOption",
                    MainOptionValue = "42"
                }
            }
            };

            page.Children.Add(firstOption);
            page.Children.Add(secondOption);
            page.Children.Add(dependentOption);

            _optionConverter.TryConvert(firstOption, _mockConverter.Object, _mockDataService.Object, out var firstOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:firstOption")))
                .Returns(firstOptionViewModel!);

            _optionConverter.TryConvert(secondOption, _mockConverter.Object, _mockDataService.Object, out var secondOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:secondOption")))
                .Returns(secondOptionViewModel!);

            _optionConverter.TryConvert(dependentOption, _mockConverter.Object, _mockDataService.Object, out var dependentOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:dependentOption")))
                .Returns(dependentOptionViewModel!);

            // Act
            _converter.TryConvert(page, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            viewModel.Should().NotBeNull();
            var pageViewModel = viewModel.Should().BeOfType<PageViewModel>().Subject;
            pageViewModel.Items.Should().NotBeNull();
            pageViewModel.Items.Should().HaveCount(3);

            var dependentViewModel = pageViewModel.Items[2].Should().BeOfType<StringViewModel>().Subject;
            dependentViewModel.DependencyViewModel.Should().NotBeNull();
            dependentViewModel.DependencyViewModel.Should().HaveCount(2);
            dependentViewModel.DependencyViewModel.Should().Contain(pageViewModel.Items[0]);
            dependentViewModel.DependencyViewModel.Should().Contain(pageViewModel.Items[1]);
        }

        [Fact]
        public void SetDependencies_WithGroupHavingDependency_SetsDependencyCorrectly()
        {
            // Arrange
            var page = new Page
            {
                Key = "parent",
                Label = "Parent Page",
                Children = new List<SettingsChildItem>(),
                Dependency = [],
            };

            var mainOption = new Option
            {
                Key = "parent:mainOption",
                Label = "Main Option",
                Type = OptionType.Boolean,
                Value = "True",
                Dependency = [],
            };

            var dependentGroup = new Group
            {
                Key = "parent:dependentGroup",
                Label = "Dependent Group",
                Children = new List<SettingsChildItem>(),
                Dependency = new List<Dependency>
        {
            new() {
                MainOptionKey = "parent:mainOption",
                MainOptionValue = "True"
            }
        }
            };

            var groupOption = new Option
            {
                Key = "parent:dependentGroup:option",
                Label = "Group Option",
                Type = OptionType.String,
                Value = "test",
                Dependency = [],
            };

            dependentGroup.Children.Add(groupOption);
            page.Children.Add(mainOption);
            page.Children.Add(dependentGroup);

            _optionConverter.TryConvert(mainOption, _mockConverter.Object, _mockDataService.Object, out var mainOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:mainOption")))
                .Returns(mainOptionViewModel!);

            _optionConverter.TryConvert(groupOption, _mockConverter.Object, _mockDataService.Object, out var groupOptionViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:dependentGroup:option")))
                .Returns(groupOptionViewModel!);

            _groupConverter.TryConvert(dependentGroup, _mockConverter.Object, _mockDataService.Object, out var dependentGroupViewModel);
            _mockConverter
                .Setup(c => c.Convert(It.Is<SettingsChildItem>(item => item.Key == "parent:dependentGroup")))
                .Returns(dependentGroupViewModel!);

            // Act
            _converter.TryConvert(page, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            viewModel.Should().NotBeNull();
            var pageViewModel = viewModel.Should().BeOfType<PageViewModel>().Subject;
            pageViewModel.Items.Should().NotBeNull();
            pageViewModel.Items.Should().HaveCount(2);

            var groupViewModel = pageViewModel.Items[1].Should().BeOfType<GroupViewModel>().Subject;
            groupViewModel.DependencyViewModel.Should().NotBeNull();
            groupViewModel.DependencyViewModel.Should().HaveCount(1);
            groupViewModel.DependencyViewModel[0].Should().BeSameAs(pageViewModel.Items[0]);
        }
    }
}