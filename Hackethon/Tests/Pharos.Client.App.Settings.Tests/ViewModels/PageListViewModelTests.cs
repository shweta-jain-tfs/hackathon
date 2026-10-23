using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.App.Settings.ViewModels.Converters;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels
{
    public class PageListViewModelTests
    {

        private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService;
        private readonly Mock<IConverter> _mockConverter;
        private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer;
        private readonly PageListViewModel _viewModel;

        public PageListViewModelTests()
        {
            _mockDataService = new Mock<ISettingsDataService<SettingItemBase>>();
            _mockConverter = new Mock<IConverter>();
            _mockLocalizer = LocalizerMockFactory.Create();

            _viewModel = new PageListViewModel(_mockDataService.Object, _mockConverter.Object, _mockLocalizer.Object);
        }

        [Fact]
        public void CanEdit_SetValue_RaisesPropertyChanged()
        {
            var wasRaised = false;
            _viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(PageListViewModel.CanEdit))
                {
                    wasRaised = true;
                }
            };

            _viewModel.CanEdit = true;

            Assert.True(wasRaised);
        }

        [Fact]
        public void CanDelete_SetValue_RaisesPropertyChanged()
        {
            var wasRaised = false;
            _viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(PageListViewModel.CanDelete))
                {
                    wasRaised = true;
                }
            };

            _viewModel.CanDelete = true;

            Assert.True(wasRaised);
        }

        [Fact]
        public void SelectedItemsChanged_SetsIsSelectedCorrectly()
        {
            var row1 = new TableRowViewModel(_mockLocalizer.Object) { IsSelected = false };
            var row2 = new TableRowViewModel(_mockLocalizer.Object) { IsSelected = false };
            _viewModel.Values.AddRange(new[] { row1, row2 });

            var selectedItems = new HashSet<TableRowViewModel> { row1 };

            _viewModel.SelectedItemsChanged(selectedItems);

            Assert.True(row1.IsSelected);
            Assert.False(row2.IsSelected);
            Assert.True(_viewModel.CanEdit);
            Assert.True(_viewModel.CanDelete);
        }

        [Fact]
        public void SelectedItemsChanged_DisablesDelete_WhenMinItemsReached()
        {
            _viewModel.Model = new PageList { MinItems = 1 };
            var row = new TableRowViewModel(_mockLocalizer.Object) { IsSelected = false };
            _viewModel.Values.Add(row);

            _viewModel.SelectedItemsChanged([row]);

            Assert.True(row.IsSelected);
            Assert.True(_viewModel.CanEdit);
            Assert.False(_viewModel.CanDelete);
        }

        [Fact]
        public void SelectedItemsChanged_AllowsDelete_WhenAboveMinItems()
        {
            _viewModel.Model = new PageList { MinItems = 1 };
            var row1 = new TableRowViewModel(_mockLocalizer.Object) { IsSelected = false };
            var row2 = new TableRowViewModel(_mockLocalizer.Object) { IsSelected = false };
            _viewModel.Values.AddRange([row1, row2]);

            _viewModel.SelectedItemsChanged([row1]);

            Assert.True(row1.IsSelected);
            Assert.False(row2.IsSelected);
            Assert.True(_viewModel.CanDelete);
        }

        [Fact]
        public async Task AddNewRow_AddsPageEntry()
        {
            _viewModel.Headers.Add("Header");
            var page = new Page { Header = "Header", Key = "root:key", Label = "Label", Children = [], Dependency = [] };
            _viewModel.PageStructure = new PageViewModel(_mockLocalizer.Object) { Model = page };

            _mockDataService.Setup(ds => ds.GetPageAsync(It.IsAny<string>(), It.IsAny<string>()))
                            .ReturnsAsync(page);
            _mockConverter.Setup(c => c.Convert(It.IsAny<SettingItemBase>())).Returns(_viewModel.PageStructure);

            var newEntry = await _viewModel.AddNewRow();

            Assert.Single(_viewModel.PageEntries);
            Assert.NotNull(newEntry);
        }

        [Fact]
        public async Task AddNewRow_UsesEntryIdUrl_WhenPlaceholderUrlIsEmpty()
        {
            var pageStructure = new PagePlaceholder
            {
                Key = "root:Entries",
                Label = "Entry",
                Url = string.Empty,
                Dependency = []
            };
            var fetchedStructure = new Page
            {
                Header = "Header",
                Key = "root:Entries",
                Label = "Entry",
                Url = "Root/Entries",
                Children = [],
                Dependency = []
            };
            _viewModel.PageStructure = new PageViewModel(_mockLocalizer.Object) { Model = pageStructure };

            _mockDataService.Setup(ds => ds.GetPageAsync("root", "root:Entries"))
                            .ReturnsAsync(fetchedStructure);
            _mockConverter.Setup(c => c.Convert(It.IsAny<SettingItemBase>()))
                          .Returns<SettingItemBase>(item => new PageViewModel(_mockLocalizer.Object) { Model = item });

            var newEntry = await _viewModel.AddNewRow();

            newEntry.Url.Should().Be("Root/0");
        }

        [Trait("ProductRequirement", "PR_1919")]
        [Trait("ModuleRequirement", "MR_3926")]
        [Fact]
        public async Task AddNewRow_UpdatesNonOverlappingRangeMetadataKeys()
        {
            var validation = new NonOverlappingRangesValidation
            {
                FirstStartKey = "root:Entries:FirstStart",
                FirstLengthKey = "root:Entries:FirstLength",
                SecondStartKey = "root:Entries:SecondStart",
                SecondLengthKey = "root:Entries:SecondLength",
                Error = nameof(SettingUpdateError.SectionsOverlap),
                EnabledWhen =
                [
                    new Dependency
                    {
                        MainOptionKey = "root:Entries:Enabled",
                        MainOptionValue = true.ToString(),
                        Type = DependencyType.Disable
                    }
                ]
            };
            var pageStructure = new PagePlaceholder
            {
                Key = "root:Entries",
                Label = "Entry",
                Url = string.Empty,
                Dependency = []
            };
            var fetchedStructure = new Page
            {
                Header = "Header",
                Key = "root:Entries",
                Label = "Entry",
                Children =
                [
                    new Option { Key = "root:Entries:Enabled", Dependency = [] },
                    new Option { Key = "root:Entries:FirstStart", Dependency = [], NonOverlappingRanges = [validation] }
                ],
                Dependency = []
            };
            _viewModel.PageStructure = new PageViewModel(_mockLocalizer.Object) { Model = pageStructure };

            _mockDataService.Setup(ds => ds.GetPageAsync("root", "root:Entries"))
                            .ReturnsAsync(fetchedStructure);
            _mockConverter.Setup(c => c.Convert(It.IsAny<SettingItemBase>()))
                          .Returns<SettingItemBase>(item => new PageViewModel(_mockLocalizer.Object) { Model = item });

            var newEntry = await _viewModel.AddNewRow();

            var option = ((Page)newEntry.Model!).Children.OfType<Option>().Single(option => option.Key == "root:Entries:0:FirstStart");
            var updatedValidation = option.NonOverlappingRanges.Single();
            new[]
            {
                updatedValidation.FirstStartKey,
                updatedValidation.FirstLengthKey,
                updatedValidation.SecondStartKey,
                updatedValidation.SecondLengthKey
            }.Should().BeEquivalentTo(
                "root:Entries:0:FirstStart",
                "root:Entries:0:FirstLength",
                "root:Entries:0:SecondStart",
                "root:Entries:0:SecondLength");
            updatedValidation.EnabledWhen.Single().MainOptionKey.Should().Be("root:Entries:0:Enabled");
        }

        [Fact]
        public async Task IsLocal_IsSetCorrectly()
        {
            // Arrange
            var pageStructure = new Page { Header = "Header", Key = "root:key", Label = "Label", Children = [], Dependency = [] };
            var pageEntry = new Page { Header = "Header", Key = "root:1:key", Label = "Label", Children = [], Dependency = [] };
            var pageEntryViewModel = new PageViewModel(_mockLocalizer.Object) { Model = pageEntry }; ;
            _viewModel.PageStructure = new PageViewModel(_mockLocalizer.Object) { Model = pageStructure };
            _viewModel.Headers = ["TestHeader"];

            _mockDataService.Setup(ds => ds.GetPageAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(pageStructure);
            _mockConverter.Setup(c => c.Convert(It.IsAny<SettingItemBase>())).Returns(pageEntryViewModel);

            // Act
            var newEntry = await _viewModel.AddNewRow();

            // Assert
            Assert.True(newEntry.IsLocal, "IsLocal should be true after adding a new entry.");

            // Act - Save the new entry
            newEntry.Save();

            // Assert
            Assert.False(newEntry.IsLocal, "IsLocal should be false after saving the entry.");
        }
    }
}
