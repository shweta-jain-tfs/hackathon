using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class MethodDetailsView
{
    [Parameter] public MethodDetailsViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool IsEditingDisabled { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }
}