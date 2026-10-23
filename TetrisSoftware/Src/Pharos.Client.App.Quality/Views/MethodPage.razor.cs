using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Breadcrumb;

namespace Pharos.Client.App.Quality.Views;

public partial class MethodPage
{
    [Parameter] public string InstrumentNumber { get; set; } = default!;
    [Parameter] public string MethodName { get; set; } = default!;

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private MethodViewModel ViewModel { get; set; } = default!;

    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new(Loc["NavMenu_Quality"], PageRoute.Quality.Default),
        new(ViewModel.InstrumentName +  " - " + MethodName, "")
    ];

    protected override async Task OnInitializedAsync()
    {
        await InstrumentFilterModel.LoadAsync();
        ViewModel.InstrumentNumber = InstrumentNumber;
        ViewModel.MethodName = MethodName;
        ViewModel.InstrumentName = InstrumentFilterModel.GetInstrumentName(InstrumentNumber);
        await ViewModel.LoadAsync();
    }
}