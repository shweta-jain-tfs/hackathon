namespace Pharos.Client.App.Parameters.QualityControl;

public partial class QualityControlPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new BreadcrumbItem(Loc["NavMenu_QualityControls"], PageRoute.Parameters.QualityControls)
    ];

    public enum TabItems
    {
        PhadiaQc,
        LabQc,
        Scheduling
    }
}