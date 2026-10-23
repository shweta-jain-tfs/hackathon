
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Settings.Services;

internal sealed class SettingsMenuProvider(ISettingsDataService<SettingItemBase> settingsDataService, IAuthorizationService authorizationService) : ISettingsMenuProvider
{
    public async Task<IReadOnlyList<RouteSubMenuItem>> GetSettingsMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        if (!await authorizationService.IsAuthorized(Permissions.EditSystemSettings))
        {
            return Array.Empty<RouteSubMenuItem>();
        }

        var menuItems = await settingsDataService.GetMenuItemsAsync();

        // TODO: Settings currently use localized labels in the URL slug (matching current module behavior).
        // In the future we might want to switch to language-invariant key-based slugs (e.g. keys in kebab-case).
        var items = menuItems
            .Select(item => new RouteSubMenuItem(
                Uri: $"{PageRoute.Settings}/{Uri.EscapeDataString(item.Value)}",
                Text: item.Value))
            .ToList();

        return items;
    }
}