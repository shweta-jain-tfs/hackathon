using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;
public partial class MethodReportsView
{
    [Parameter] public MethodReportViewModel ReportViewModel { get; set; } = default!;

    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private bool IsEditingDisabled { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    private string GetManufacturerDefaultText(string? value)
    {
        return string.Format(Loc["MethodDetailsReportTab_ManufacturerDefaultFormat"], value ?? string.Empty);
    }

    private string GetManufacturerDefaultText(double? value)
    {
        return string.Format(Loc["MethodDetailsReportTab_ManufacturerDefaultFormat"], value?.ToString() ?? string.Empty);
    }
}