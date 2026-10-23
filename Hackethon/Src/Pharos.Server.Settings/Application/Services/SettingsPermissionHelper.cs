using Pharos.Server.Application.Security;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal class SettingsPermissionHelper(IAuthorizationService authService) : ISettingsPermissionHelper
{
    /// <summary>
    /// Preloads all permissions for the current user to avoid calling the authorization service recursively during runtime.
    /// <param name="items"></param>
    /// </summary>
    public async Task<HashSet<string>> PreloadPermissionsAsync(IEnumerable<SettingItemBase> items)
    {
        HashSet<string> cachedPermissions = [];
        var permissionsToCheck = items
            .SelectMany(i => i.Permissions)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();

        foreach (var permission in permissionsToCheck)
        {
            if (await authService.AuthorizeCurrentUserAsync(permission))
            {
                cachedPermissions.Add(permission);
            }
        }

        return cachedPermissions;
    }

    /// <summary>
    /// Checks if the current user has permission to access the given settings item (using cached permissions where applicable)
    /// </summary>
    /// <param name="item"></param>
    /// /// <param name="cachedPermissions"></param>
    /// <returns></returns>
    public async Task<bool> HasPermissionAsync(SettingItemBase item, HashSet<string>? cachedPermissions = null)
    {
        if (item.ShowOnlyInRndMode && !authService.RndMode)
        {
            return false;
        }

        var required = item.Permissions
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Distinct()
        .ToArray();

        if (required.Length == 0)
        {
            return true;
        }

        if (cachedPermissions != null)
        {
            return required.All(cachedPermissions.Contains);
        }

        // Fallback to direct authorization check if permissions are not preloaded
        return await authService.AuthorizeCurrentUserAsync(required);
    }

    /// <summary>
    /// Removes disallowed children recursively from the given parent item.
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public async Task RemoveInaccessibleChildrenAsync(ISettingsParentItem parent)
    {
        for (var i = parent.Children.Count - 1; i >= 0; i--)
        {
            var child = parent.Children[i];
            if (!await HasPermissionAsync(child))
            {
                parent.Children.RemoveAt(i);
                continue;
            }

            switch (child)
            {
                case Page childPage:
                    await RemoveInaccessibleChildrenAsync(childPage);
                    break;
                case Group childGroup:
                    await RemoveInaccessibleChildrenAsync(childGroup);
                    break;
                case PageList pageList:
                    await PrunePageListAsync(pageList);
                    break;
            }
        }
    }

    /// <summary>
    /// Removes disallowed pages from the given page list.
    /// </summary>
    /// <param name="pageList"></param>
    /// <returns></returns>
    private async Task PrunePageListAsync(PageList pageList)
    {
        if (pageList.PageStructure is Page structurePage)
        {
            await RemoveInaccessibleChildrenAsync(structurePage);
        }

        for (var j = pageList.PageEntries.Count - 1; j >= 0; j--)
        {
            if (pageList.PageEntries[j] is Page pageEntry)
            {
                await RemoveInaccessibleChildrenAsync(pageEntry);
            }
        }

        PageListColumnHelper.Rebuild(pageList);
    }
}
