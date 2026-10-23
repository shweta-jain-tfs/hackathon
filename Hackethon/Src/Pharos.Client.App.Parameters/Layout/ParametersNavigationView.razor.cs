using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Layout;

public partial class ParametersNavigationView
{
    private UiSettings? _uiSettings;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;

    private bool CanViewRequestorInfo =>
        HasViewPersonalInfoPermission &&
        _uiSettings?.RequestSettings?.IsRequestorInformationEnabled == true;

    private bool HasViewPersonalInfoPermission { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _uiSettings = await UiSettingsService.GetSettingsAsync();
        HasViewPersonalInfoPermission = await AuthorizationService.IsAuthorized(Permissions.ViewPersonalAndHealthData);
    }
}