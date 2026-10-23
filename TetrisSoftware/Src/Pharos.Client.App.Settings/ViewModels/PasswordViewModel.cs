using Microsoft.Extensions.Localization;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.Settings.ViewModels;

public class PasswordViewModel(IStringLocalizer<Translations> loc, ISettingsDataService<SettingItemBase> dataService) : StringViewModel(loc, dataService)
{
    public bool IsCleared { get; private set; }

    public void ClearOnFirstInteraction()
    {
        if (!IsCleared)
        {
            Value = string.Empty;
            IsCleared = true;
        }
    }
}
