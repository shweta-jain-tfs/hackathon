namespace Pharos.Client.App.Parameters.Common.Views;

public partial class ConfigurationPage
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = null!;

    private List<BreadcrumbItem> Breadcrumbs { get; set; } = [];

    protected override void OnInitialized()
    {
        Breadcrumbs =
        [
            new(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new(Loc["NavMenu_Configuration"], PageRoute.Parameters.Configuration),
        ];
    }
}