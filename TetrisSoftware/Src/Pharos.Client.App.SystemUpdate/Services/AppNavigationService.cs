using Microsoft.AspNetCore.Components;

namespace Pharos.Client.App.SystemUpdate.Services;

public sealed class AppNavigationService(NavigationManager navigationManager) : IAppNavigationService
{
    public void Reload()
    {
        navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true);
    }
}
