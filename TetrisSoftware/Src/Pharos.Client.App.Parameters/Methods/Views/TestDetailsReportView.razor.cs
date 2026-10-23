using Pharos.Client.App.Parameters.Methods.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Methods.Views;

public partial class TestDetailsReportView
{
    [Parameter] public MethodTestDetailsReportViewModel ReportViewModel { get; set; } = default!;
    [CascadingParameter] private EditContext? EditContext { get; set; }
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool IsEditingDisabled { get; set; }

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    private string GetManufacturerDefaultText(string? value)
    {
        return string.Format(Loc["MethodDetailsReportTab_ManufacturerDefaultFormat"], value ?? string.Empty);
    }

    private string GetManufacturerDefaultText(bool value)
    {
        return GetManufacturerDefaultText(value ? Loc["MethodDetailsTestTab_Yes"] : Loc["MethodDetailsTestTab_No"]);
    }

    private string GetManufacturerDefaultText(double? value)
    {
        return string.Format(Loc["MethodDetailsReportTab_ManufacturerDefaultFormat"], value?.ToString() ?? string.Empty);
    }

    private string GetManufacturerDefaultText(int value)
    {
        return string.Format(Loc["MethodDetailsReportTab_ManufacturerDefaultFormat"], value.ToString());
    }

    private string GetManufacturerDefaultText(QuotientReportMode value)
    {
        return GetManufacturerDefaultText(GetReportQuotientText(value));
    }

    private static string GetReportQuotientText(QuotientReportMode value)
    {
        return value.ToLocalizedString("MethodDetailsTestTab");
    }

    private List<DropDownOption<int>> GetReportClassIdDropDownValues()
    {
        return ReportViewModel.ReportClassIdDropDownValues
            .Select(x => new DropDownOption<int>
            {
                Text = x.Value == 0 ? Loc["MethodDetailsTestReportTab_ClassTableNone"] : x.Text,
                Value = x.Value
            })
            .ToList();
    }

    private void OnReportClassChanged(bool _)
    {
        EditContext?.NotifyFieldChanged(FieldIdentifier.Create(() => ReportViewModel.ReportClassId));
    }

    private bool IsReportClassIdInvalid()
    {
        return ReportViewModel.ReportClass && ReportViewModel.ReportClassId == 0;
    }
}
