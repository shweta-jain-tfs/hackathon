using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class TableEntryViewModel(IStringLocalizer<Translations> loc) : SettingsViewModelBase(loc)
{
    public SettingsViewModelBase? Representation { get; set; }

    public PagePlaceholder? PagePlaceholder { get; set; }

    public string? Header { get; set; }
    public string? Value { get; set; }
}