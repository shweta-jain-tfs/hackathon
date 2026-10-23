namespace Pharos.Client.App.Result.Views;

public partial class CurveControlDetailsPage
{
    [Parameter] public int Id { get; set; }
    [Inject] private MethodNameViewModel MethodNameViewModel { get; set; } = default!;
    private string MethodName { get; set; } = string.Empty;
    private string InstrumentSerial { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var info = await MethodNameViewModel.GetMethodNameForRun(Id);
        MethodName = info.MethodName;
        InstrumentSerial = info.InstrumentNumber;
    }

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem("Results", PageRoute.Results),
        new BreadcrumbItem(MethodName, $"{PageRoute.AnalyticalRunDetails.Default}/{Id}"),
        new BreadcrumbItem("Curve Controls", "")
    ];
}