using Pharos.Client.App.Parameters.Packages.ViewModels;

namespace Pharos.Client.App.Parameters.Packages;

public partial class PackagesPage
{
    private Dictionary<string, string> _marketNameDict = [];
    [Inject] private PackagesViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new BreadcrumbItem(Loc["NavMenu_Products"], PageRoute.Parameters.Packages)
    ];

    protected override async Task OnInitializedAsync()
    {
        _marketNameDict = await ViewModel.GetMarketNamesAsync();
    }
}