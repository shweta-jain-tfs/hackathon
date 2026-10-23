using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Pharos.Contracts;
using Pharos.Server.Application.Settings.General;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Components;

public partial class App
{
    private const string _appTitle = "Phadia E Operator Software";

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [Inject] private IIdentityRedirectManager RedirectManager { get; set; } = default!;
    [Inject] IOptionsSnapshot<GeneralSettings> GeneralOptions { get; set; } = default!;
    [Inject] IClientDeviceService ClientDeviceService { get; set; } = default!;

    private ClientDeviceType ClientDeviceType => ClientDeviceService.ClientDeviceType;
    // Needed for server pages (theme + touch)
    private string? DeviceThemeCssClass => ClientDeviceService.ClientDeviceType == ClientDeviceType.Instrument ? "ph-theme-instrument" : null;
    private string? PreventContextMenuOnInstrument => ClientDeviceService.ClientDeviceType == ClientDeviceType.Instrument ? "return false;" : null;

    private bool IsIdentityPage => RedirectManager.IsPathIdentityPage(HttpContext.Request.Path);
    private IComponentRenderMode? RenderModeForPage => IsIdentityPage ? null : new InteractiveWebAssemblyRenderMode(prerender: false);

    // ReSharper disable once InconsistentNaming
    private CultureInfo UICulture { get; set; } = CultureInfo.InvariantCulture;
    private string CultureName => UICulture.Name;


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        UICulture = GeneralOptions.Value.GetCultureInfoFromSetting();

        // Set request culture for each user's API calls
        HttpContext?.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(UICulture, UICulture)));

#if !NSWAG
        if (!IsIdentityPage)
        {
            await RedirectManager.RedirectToRegisterIfNotInitialized(null);
        }
#endif
    }
}
