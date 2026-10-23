using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Interfaces;

internal interface ISettingsPermissionHelper
{
    Task<HashSet<string>> PreloadPermissionsAsync(IEnumerable<SettingItemBase> items);
    Task<bool> HasPermissionAsync(SettingItemBase item, HashSet<string>? cachedPermissions = null);
    Task RemoveInaccessibleChildrenAsync(ISettingsParentItem parent);
}