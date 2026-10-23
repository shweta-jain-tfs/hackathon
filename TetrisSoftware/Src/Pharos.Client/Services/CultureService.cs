using System.Globalization;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Contexts;

namespace Pharos.Client.Services;

internal class CultureService
{
    private readonly UiSettingsService _uISettings;
    private CultureInfo _culture = CultureInfo.CurrentCulture;

    internal UiContext UiContext { get; private set; } = null!;

    public CultureService(UiSettingsService uISettings)
    {
        _uISettings = uISettings;
    }

    public async Task SetCultureAsync()
    {
        var uiSettings = await _uISettings.GetSettingsAsync();
        var cultureName = uiSettings.RegionalSettings.Culture;
        var currentCulture = _culture;

        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            try
            {
                _culture = CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                _culture = CultureInfo.CurrentCulture;
            }
        }
        else
        {
            _culture = CultureInfo.CurrentCulture;
        }

        if (currentCulture.LCID != _culture.LCID)
        {
            ApplyCulture(_culture);
        }
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}