using System.Web;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Contracts.Security;

namespace Pharos.Client.Views;

/// <summary>
/// Logout page for manual logouts by the user. Navigating to this page will trigger any navigation lock
/// that's on the current page so that the user can save or discard changes before logging out.
/// </summary>
public partial class LogoutPage
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        var uri = new Uri(Navigation.Uri);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);
        var returnUrl = queryParams["returnUrl"] ?? PageRoute.Tasks;
        if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = PageRoute.Tasks;
        }

        Navigation.NavigateTo($"{AccountRoutes.Logout}?returnUrl={Uri.EscapeDataString(returnUrl)}",
                     new NavigationOptions
                     {
                         ForceLoad = true,
                         ReplaceHistoryEntry = true
                     });
    }
}