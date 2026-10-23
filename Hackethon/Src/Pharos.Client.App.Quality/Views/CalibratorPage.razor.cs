using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Breadcrumb;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Quality.Views;

public partial class CalibratorPage : IDisposable
{
    [Parameter] public string InstrumentNumber { get; set; } = string.Empty;
    [Parameter] public string MethodName { get; set; } = string.Empty;

    [Inject] private CurvesViewModel ViewModel { get; set; } = default!;
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private string InstrumentName { get; set; } = string.Empty;
    private List<int> _selectedCurveIds = [];

    protected override async Task OnInitializedAsync()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;

        await InstrumentFilterModel.LoadAsync();
        ViewModel.InstrumentNumber = InstrumentNumber;
        ViewModel.MethodName = MethodName;
        InstrumentName = InstrumentFilterModel.GetInstrumentName(InstrumentNumber);

        ViewModel.IsLoaded = true;
    }

    protected override async Task OnParametersSetAsync()
    {
        await Task.CompletedTask;
    }

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new(Loc["NavMenu_Quality"], PageRoute.Quality.Default),
        new BreadcrumbItem( InstrumentName + " - " + MethodName, $"{PageRoute.Quality.Default}/{InstrumentNumber}/{MethodName}"),
        new BreadcrumbItem("Calibrators", "")
    ];

    public bool NoCurvesSelected => _selectedCurveIds.Count == 0;

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();

        GC.SuppressFinalize(this);
    }

    private static string GetStatusText(CurvePresentation curve)
    {
        // CC failed
        if (curve.CurveControlDisabledFlag)
        {
            return "CC failed";
        }

        // Curve flagged
        if (curve.Judgment == CurveJudgment.Flagged)
        {
            return "Flagged";
        }

        // An Active curve
        if (curve.ActiveCurve)
        {
            return "Active";
        }

        // Not curve fitted
        if (curve.Judgment == CurveJudgment.NotOK || !curve.CurveFittedFlag)
        {
            return "Calibration failed";
        }

        // TODO: Curve expired

        // Curve OK
        return "OK";
    }

    private void OnSelectedItemsChanged(HashSet<CurvePresentation> curves)
    {
        _selectedCurveIds = curves.Select(x => x.Id).ToList();
    }
}