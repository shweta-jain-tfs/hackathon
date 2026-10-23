using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class TestDetailsGeneralView
{
    [Parameter] public MethodTestDetailsGeneralViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool IsEditingDisabled { get; set; }

    private string GetManufacturerDefaultForDilutionMethod()
    {
        return string.Format(Loc["TestDetailsGeneralTab_ManufacturerDefault"], ViewModel.Model.DefaultDilutionMethod.ToLocalizedString("TestDetailsGeneralTab"));
    }

    private string GetManufacturerDefaultForDilutionFactor()
    {
        return string.Format(Loc["TestDetailsGeneralTab_ManufacturerDefault"], ViewModel.Model.DefaultDilutionFactor);
    }

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }
}