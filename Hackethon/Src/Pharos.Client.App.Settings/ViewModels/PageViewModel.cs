using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;
public class PageViewModel(IStringLocalizer<Translations> loc) : ContainerSettingsViewModelBase(loc)
{
    public string Header => ((Page)Model!).Header;

    public override bool Save()
    {
        foreach (var item in Items)
        {
            // Do not save hidden or disabled settings
            if (item.IsDependencyEnabled)
            {
                item.Save();
            }
        }

        IsModified = false;
        IsLocal = false;

        return true;
    }
}
