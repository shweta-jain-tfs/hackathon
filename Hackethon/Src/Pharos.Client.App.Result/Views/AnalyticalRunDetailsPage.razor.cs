using Microsoft.Extensions.Localization;
using Pharos.Client.App.Result.Models;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Result.Views;

public partial class AnalyticalRunDetailsPage : IDisposable
{
    [Parameter] public int Id { get; set; }
    [SupplyParameterFromQuery(Name = "TestOrderId")] public Guid? TestOrderId { get; set; }
    [Inject] private MethodNameViewModel MethodNameViewModel { get; set; } = default!;
    [Inject] private CurveViewModel CurveViewModel { get; set; } = default!;
    [Inject] public IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    private string? MethodName { get; set; }
    private string? InstrumentSerial { get; set; }
    private bool IsTableExpanded { get; set; }
    private bool IsNarrow => ChartStateService.TotalCharts > 6;
    [Inject] private ChartStateService ChartStateService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var info = await MethodNameViewModel.GetMethodNameForRun(Id);
        MethodName = info.MethodName;
        InstrumentSerial = info.InstrumentNumber;
        ChartStateService.OnChange += StateHasChanged;
    }
    
    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem("Results", PageRoute.Results),
        new BreadcrumbItem(MethodName ?? "", "")
    ];

    private async Task SelectCurveOpen()
    {
        CurveViewModel.AnalyticalRunId = Id;

        await CurveViewModel.OpenSelectCurveModal(ModalService, Loc);
        StateHasChanged();
    }

    public void Dispose()
    {
        ChartStateService.OnChange -= StateHasChanged;
        ChartStateService.TotalCharts = 0;
        GC.SuppressFinalize(this);
    }
}