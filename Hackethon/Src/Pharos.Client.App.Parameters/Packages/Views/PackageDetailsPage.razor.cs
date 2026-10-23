using Pharos.Client.App.Parameters.Packages.ViewModels;

namespace Pharos.Client.App.Parameters.Packages.Views;

public partial class PackageDetailsPage : EntityEditorPageBase<PackageDetailsViewModel, PackageDetails>
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private string Title => $"{ViewModel.Entity.OrderingNumber} : {ViewModel.Entity.PackageFullName}" ?? Loc["PackageDetailsPage_Header"];

    protected override List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new BreadcrumbItem(Loc["NavMenu_Products"], PageRoute.Parameters.Packages),
        new BreadcrumbItem(Title, "")
    ];
    private IQueryable<Package> SelectedPackage => new[] { ViewModel.Entity }.AsQueryable();

    protected override void NavigateToParentList()
    {
        Navigation.NavigateTo(PageRoute.Parameters.Packages);
    }
}