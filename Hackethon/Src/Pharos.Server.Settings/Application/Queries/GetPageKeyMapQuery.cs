using MediatR;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Queries;

internal record GetPageKeyMapQuery : IRequest<Dictionary<string, string>>;

internal class GetPageKeyMapQueryHandler : IRequestHandler<GetPageKeyMapQuery, Dictionary<string, string>>
{
    private readonly ISettingsPermissionHelper _permissionHelper;
    private readonly ISettingsStringLocalizer _localizer;
    private readonly ISettingsPagesLoader _loader;

    public GetPageKeyMapQueryHandler(ISettingsPermissionHelper permissionHelper, ISettingsStringLocalizer localizer, ISettingsPagesLoader loader)
    {
        _permissionHelper = permissionHelper;
        _localizer = localizer;
        _loader = loader;
    }

    public async Task<Dictionary<string, string>> Handle(GetPageKeyMapQuery request, CancellationToken cancellationToken)
    {
        var pages = SettingsPagesProvider.GetAllPages(_localizer);

        var allowedRootKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in pages.Values)
        {
            if (!await _permissionHelper.HasPermissionAsync(page))
            {
                continue;
            }

            allowedRootKeys.Add(page.Key);
        }

        var populatedPages = await _loader.LoadAllAsync(cancellationToken);

        // Mapping pages only to allowed root pages (prevents deep linking navigation to disallowed pages)
        var pageKeyMap = new Dictionary<string, string>();
        foreach (var p in populatedPages.Values)
        {
            var rootKey = p.Key.Split(PageHelper.SettingsKeyDeLimiter)[0];
            if (!allowedRootKeys.Contains(rootKey) || !await _permissionHelper.HasPermissionAsync(p))
            {
                continue;
            }
            pageKeyMap[p.Url] = p.Key;
            await AddGroupUrlsAsync(pageKeyMap, p, p.Key);
        }

        return pageKeyMap;
    }

    private async Task AddGroupUrlsAsync(
        Dictionary<string, string> pageKeyMap,
        ISettingsParentItem parent,
        string pageKey)
    {
        foreach (var child in parent.Children)
        {
            switch (child)
            {
                case Group group:
                    if (!await _permissionHelper.HasPermissionAsync(group))
                    {
                        break;
                    }

                    pageKeyMap[group.Url] = pageKey;
                    await AddGroupUrlsAsync(pageKeyMap, group, pageKey);
                    break;
                case Page:
                    break;
                case PageList:
                    break;
            }
        }
    }
}