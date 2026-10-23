using Microsoft.Extensions.Localization;
using Pharos.Client.App.Settings.Models;
using Pharos.Client.App.Settings.ViewModels.Converters;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;
using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Settings.ViewModels
{
    public class SettingsMainViewModelTests_DeletePageListEntry
    {
        private static SettingsMainViewModel CreateSut(Mock<ISettingsDataService<SettingItemBase>> dataServiceMock)
        {
            var converterMock = new Mock<IConverter>();
            var locMock = new Mock<IStringLocalizer<Translations>>();
            return new SettingsMainViewModel(dataServiceMock.Object, converterMock.Object, locMock.Object);
        }

        private static PageListViewModel CreatePageListViewModel(
            Mock<ISettingsDataService<SettingItemBase>> dataServiceMock,
            IEnumerable<string?> selectedEntryKeys,
            IEnumerable<(string? key, string label)> pageEntries,
            IEnumerable<(string? key, string defaultValue, string currentValue, List<DropDownOption<string>> choices)> dropdowns)
        {
            var converterMock = new Mock<IConverter>();
            var locMock = new Mock<IStringLocalizer<Translations>>();
            var vm = new PageListViewModel(dataServiceMock.Object, converterMock.Object, locMock.Object)
            {
                Values = selectedEntryKeys
                    .Select(k => new TableRowViewModel(locMock.Object) { EntryKey = k, IsSelected = true, Values = new List<TableEntryViewModel>() })
                    .ToList(),
                PageEntries = pageEntries
                    .Select(pe => new PagePlaceholder { Key = pe.key, Label = pe.label })
                    .ToList(),
                // IMPORTANT: Key and DefaultValue come from the underlying Model; set Model to make Key non-null
                DynamicDropdowns = dropdowns
                    .Select(dd =>
                        new ChoicesViewModel(locMock.Object, dataServiceMock.Object)
                        {
                            Choices = dd.choices,
                            Model = new Option
                            {
                                Key = dd.key ?? string.Empty,
                                DefaultValue = dd.defaultValue
                            },
                            Value = dd.currentValue
                        }
                    )
                    .ToList()
            };

            return vm;
        }

        [Fact]
        public async Task DeletePageListEntry_NoSelection_DoesNotCallDelete()
        {
            var dataServiceMock = new Mock<ISettingsDataService<SettingItemBase>>();
            var sut = CreateSut(dataServiceMock);

            var vm = CreatePageListViewModel(
                dataServiceMock,
                selectedEntryKeys: Enumerable.Empty<string?>(),
                pageEntries: Enumerable.Empty<(string? key, string label)>(),
                dropdowns: Enumerable.Empty<(string? key, string defaultValue, string currentValue, List<DropDownOption<string>> choices)>()
            );

            await sut.DeletePageListEntry(vm);

            dataServiceMock.Verify(s => s.DeletePagesAsync(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task DeletePageListEntry_SelectedEntries_DeletesSelectedKeysOnly()
        {
            var selectedKey = "entry-1";
            var dataServiceMock = new Mock<ISettingsDataService<SettingItemBase>>();
            var sut = CreateSut(dataServiceMock);

            var vm = CreatePageListViewModel(
                dataServiceMock,
                selectedEntryKeys: new[] { selectedKey },
                pageEntries: new List<(string? key, string label)> { (selectedKey, "Label A") },
                dropdowns: Enumerable.Empty<(string? key, string defaultValue, string currentValue, List<DropDownOption<string>> choices)>()
            );

            await sut.DeletePageListEntry(vm);

            dataServiceMock.Verify(s => s.DeletePagesAsync(It.Is<string[]>(arr => arr.Length == 1 && arr[0] == selectedKey)), Times.Once);
            dataServiceMock.Verify(s => s.DeletePagesAsync(It.Is<string[]>(arr => arr.Length == 0)), Times.Never);
        }

        [Fact]
        public async Task DeletePageListEntry_DropdownMatchesButAtDefault_DoesNotClearDropdown()
        {
            var selectedKey = "entry-3";
            var dropdownKey = "dropdown-2";

            var dataServiceMock = new Mock<ISettingsDataService<SettingItemBase>>();
            var sut = CreateSut(dataServiceMock);

            var vm = CreatePageListViewModel(
                dataServiceMock,
                selectedEntryKeys: new[] { selectedKey },
                pageEntries: new List<(string? key, string label)> { (selectedKey, "Label C") },
                dropdowns: new List<(string? key, string defaultValue, string currentValue, List<DropDownOption<string>> choices)>
                {
                        (
                            key: dropdownKey,
                            defaultValue: "entry-3",
                            currentValue: "entry-3",
                            choices: new List<DropDownOption<string>>
                            {
                                // Text matches but value equals default -> should not clear
                                new() { Text = "Label C", Value = "entry-3" }
                            }
                        )
                }
            );

            await sut.DeletePageListEntry(vm);

            dataServiceMock.Verify(s => s.DeletePagesAsync(It.Is<string[]>(arr => arr.SequenceEqual(new[] { selectedKey }))), Times.Once);
            dataServiceMock.Verify(s => s.DeletePagesAsync(It.Is<string[]>(arr => arr.Length == 1 && arr[0] == dropdownKey)), Times.Never);
        }
    }
}
