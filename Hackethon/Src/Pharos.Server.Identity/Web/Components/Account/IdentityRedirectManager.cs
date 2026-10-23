using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Web.Components.Account;

internal sealed class IdentityRedirectManager(
    NavigationManager navigationManager,
    IApplicationUserManager userManager) : IIdentityRedirectManager
{
    public bool IsPathIdentityPage(PathString path)
    {
        return path.StartsWithSegments("/Account");
    }

    public void RedirectTo(string? uri)
    {
        uri ??= "";

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        // In .NET 10 projects that disable navigation exceptions, this won't throw during static SSR.
        navigationManager.NavigateTo(uri, forceLoad: true); // full HTTP request
    }

    public async Task RedirectToRegisterIfNotInitialized(string? returnUrl)
    {
        if (!await userManager.AreUsersInitialized())
        {
            // the route is not a constant because it's temporary
            RedirectTo("Account/Register", new() { ["returnUrl"] = returnUrl });
        }
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
        RedirectTo(newUri);
    }
}
