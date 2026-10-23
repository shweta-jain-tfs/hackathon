using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Client.App.Settings.TestHelpers;

namespace Pharos.Client.App.Settings.ViewModels.Converters
{
    public class PageListConverterTests
    {
        private readonly Mock<IStringLocalizer<Translations>> _mockLocalizer;
        private readonly Mock<IConverter> _mockConverter;
        private readonly Mock<ISettingsDataService<SettingItemBase>> _mockDataService;
        private readonly PageListConverter _converter;

        public PageListConverterTests()
        {
            _mockLocalizer = LocalizerMockFactory.Create();
            _mockConverter = new Mock<IConverter>();
            _mockDataService = new Mock<ISettingsDataService<SettingItemBase>>();
            _converter = new PageListConverter(_mockLocalizer.Object);
        }

        [Fact]
        public void Type_ShouldReturnPageListType()
        {
            Assert.Equal(typeof(PageList), _converter.Type);
        }

        [Fact]
        public void TryConvert_InvalidItem_ReturnsFalse()
        {
            var result = _converter.TryConvert(new SettingItemBase(), _mockConverter.Object, _mockDataService.Object, out var viewModel);

            result.Should().BeFalse();
            viewModel.Should().BeNull();
        }

        [Fact]
        public void TryConvert_PageListNoItems_ReturnsTrueAndCreatesViewModel()
        {
            // Arrange
            var model = new PageList
            {
                Key = "testkey",
                Label = "aLabel",
                Url = "root/aLabel",
                PageEntries = [],
                PageStructure = new PagePlaceholder { Label = "ugg" },
                ColumnHeaderLabels = [],
                Columns = new List<Row>(),
                DynamicDropdowns = [],
            };

            var aLocalizedLabel = "local label";

            _mockLocalizer.Setup(m => m[model.Label]).Returns(new LocalizedString(aLocalizedLabel, aLocalizedLabel));

            // Act
            var result = _converter.TryConvert(model, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            result.Should().BeTrue();
            viewModel.Should().NotBeNull();
            viewModel!.Should().BeOfType<PageListViewModel>();
            viewModel!.Label.Should().Be("aLabel");
            viewModel!.Key.Should().Be(model.Key);
        }

        [Fact]
        public void TryConvert_PageListWithItems_ReturnsTrueAndCreatesViewModel()
        {
            // Arrange
            var model = new PageList
            {
                Key = "testkey",
                Label = "aLabel",
                Url = "root/aLabel",
                PageEntries = new List<ChildPageItem>
                {
                    new PagePlaceholder{Key = "testkey:child1", Label = "aName"},
                    new PagePlaceholder{Key = "testkey:child1", Label = "aName"}
                },
                PageStructure = new PagePlaceholder { Label = "ugg" },
                ColumnHeaderLabels = [],
                Columns = new List<Row>(),
                DynamicDropdowns = [],
            };

            var aLocalizedLabel = "local label";

            _mockLocalizer.Setup(m => m[model.Label]).Returns(new LocalizedString(aLocalizedLabel, aLocalizedLabel));

            // Act
            var result = _converter.TryConvert(model, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            result.Should().BeTrue();
            viewModel.Should().NotBeNull();
            viewModel!.Should().BeOfType<PageListViewModel>();
            var pageListViewModel = viewModel.As<PageListViewModel>();
            viewModel!.Label.Should().Be("aLabel");
            viewModel.Key.Should().Be(model.Key);
            pageListViewModel.PageEntries.Count.Should().Be(2);
            foreach (var modelPageEntry in model.PageEntries)
            {
                pageListViewModel.PageEntries.First(e => e.Key == modelPageEntry.Key).Label.Should().Be(modelPageEntry.Label);
            }
        }

        [Fact]
        public void TryConvert_ProviderManagedPageList_UsesEntryLabelForNameColumn()
        {
            // Arrange
            var model = new PageList
            {
                Key = "testkey",
                Label = "aLabel",
                Url = "root/aLabel",
                IsProviderManaged = true,
                PageEntries =
                [
                    new PagePlaceholder { Key = "testkey:0", Label = "Instrument A", Url = "root/aLabel/Instrument A" }
                ],
                PageStructure = new PagePlaceholder { Label = "ugg" },
                ColumnHeaderLabels = ["Name", "Volume"],
                Columns =
                [
                    new Row
                    {
                        ColumnHeaderLabel = "Name",
                        Value = new Option
                        {
                            Key = "testkey:0:Name",
                            Value = "Stale instrument name",
                            IsNameColumn = true,
                            IsReadOnly = true
                        }
                    },
                    new Row
                    {
                        ColumnHeaderLabel = "Volume",
                        Value = new Option
                        {
                            Key = "testkey:0:Waste:Volume",
                            Value = "10",
                            Type = OptionType.Integer
                        }
                    }
                ],
                DynamicDropdowns = [],
            };

            // Act
            var result = _converter.TryConvert(model, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            result.Should().BeTrue();
            var pageListViewModel = viewModel.Should().BeOfType<PageListViewModel>().Subject;
            var row = pageListViewModel.Values.Should().ContainSingle().Subject;

            row.GetValue("Name").Value.Should().Be("Instrument A");
            row.GetValue("Name").Model.Should().BeOfType<PagePlaceholder>();
            row.GetValue("Volume").Value.Should().Be("10");
            row.Values.Should().NotContain(v => v.Value == "Stale instrument name");
        }

        [Fact]
        public void TryConvert_ProviderManagedPageList_UsesEntryLabelWhenNameColumnIsEditable()
        {
            // Arrange
            var model = new PageList
            {
                Key = "testkey",
                Label = "aLabel",
                Url = "root/aLabel",
                IsProviderManaged = true,
                PageEntries =
                [
                    new PagePlaceholder { Key = "testkey:0", Label = "Provider label", Url = "root/aLabel/Provider label" }
                ],
                PageStructure = new PagePlaceholder { Label = "ugg" },
                ColumnHeaderLabels = ["Name"],
                Columns =
                [
                    new Row
                    {
                        ColumnHeaderLabel = "Name",
                        Value = new Option
                        {
                            Key = "testkey:0:Name",
                            Value = "Editable setting name",
                            IsNameColumn = true
                        }
                    }
                ],
                DynamicDropdowns = [],
            };

            // Act
            var result = _converter.TryConvert(model, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            result.Should().BeTrue();
            var pageListViewModel = viewModel.Should().BeOfType<PageListViewModel>().Subject;
            var row = pageListViewModel.Values.Should().ContainSingle().Subject;

            row.GetValue("Name").Value.Should().Be("Provider label");
            row.GetValue("Name").Model.Should().BeOfType<PagePlaceholder>();
            row.Values.Should().NotContain(v => v.Value == "Editable setting name");
        }

        [Fact]
        public void TryConvert_ProviderManagedPageList_UsesEntryLabelWhenNameColumnDoesNotExist()
        {
            // Arrange
            var model = new PageList
            {
                Key = "testkey",
                Label = "aLabel",
                Url = "root/aLabel",
                IsProviderManaged = true,
                PageEntries =
                [
                    new PagePlaceholder { Key = "testkey:0", Label = "Provider label", Url = "root/aLabel/Provider label" }
                ],
                PageStructure = new PagePlaceholder { Label = "ugg" },
                ColumnHeaderLabels = ["Name", "Enabled"],
                Columns =
                [
                    new Row
                    {
                        ColumnHeaderLabel = "Enabled",
                        Value = new Option
                        {
                            Key = "testkey:0:Enabled",
                            Value = "true",
                            Type = OptionType.Boolean
                        }
                    }
                ],
                DynamicDropdowns = [],
            };

            // Act
            var result = _converter.TryConvert(model, _mockConverter.Object, _mockDataService.Object, out var viewModel);

            // Assert
            result.Should().BeTrue();
            var pageListViewModel = viewModel.Should().BeOfType<PageListViewModel>().Subject;
            var row = pageListViewModel.Values.Should().ContainSingle().Subject;

            row.GetValue("Name").Value.Should().Be("Provider label");
            row.GetValue("Name").Model.Should().BeOfType<PagePlaceholder>();
            row.GetValue("Enabled").Value.Should().Be("true");
        }



    }
}
