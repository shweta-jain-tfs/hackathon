using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;
public partial class MethodCalibratorsView
{
    [Parameter] public MethodCalibratorsViewModel ViewModel { get; set; } = default!;
    [Parameter] public bool AllowMonthlyCalibration { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool IsEditingDisabled { get; set; }
    private bool IsMaxNumberOfDaysEditingDisabled => IsEditingDisabled || !ViewModel.AllowMonthlyCalibration;

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    private string GetManufacturerDefaultForMaxNumberOfDays()
    {
        return string.Format(Loc["MethodDetailsCalibratorsTab_ManufacturerDefault"], ViewModel.AllowMonthlyCalibration ? ViewModel.Model.DefaultMaxNumberOfDays : 0);
    }
}