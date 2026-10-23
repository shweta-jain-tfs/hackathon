using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Quality.ViewModels;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.ViewModels;
using Pharos.Common.Web.Components.Breadcrumb;
using Pharos.Common.Web.Components.Layout;

namespace Pharos.Client.App.Quality.Views;

public partial class CurveControlPage : IDisposable
{
    [Parameter] public string InstrumentNumber { get; set; } = string.Empty;
    [Parameter] public string MethodName { get; set; } = string.Empty;

    [Inject] private CurveControlViewModel ViewModel { get; set; } = default!;
    [Inject] private InstrumentFilterViewModel InstrumentFilterModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private bool SingleChart => ColumnSpan == ColumnSpan.Full;
    private string InstrumentName { get; set; } = string.Empty;
    protected List<BreadcrumbItem> Breadcrumbs =>
    [
        new(Loc["NavMenu_Quality"], PageRoute.Quality.Default),
        new BreadcrumbItem( InstrumentName + " - " + MethodName, $"{PageRoute.Quality.Default}/{InstrumentNumber}/{MethodName}"),
        new BreadcrumbItem("Curve Controls", "")
    ];

    public ColumnSpan ColumnSpan
    {
        get
        {
            if (ViewModel.ChartHolders.Count < 2)
            {
                return ColumnSpan.Full;
            }
            else
            {
                return ColumnSpan.Half;
            }
        }
    }


    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += OnPropertyChanged;

        await InstrumentFilterModel.LoadAsync();
        ViewModel.InstrumentNumber = InstrumentNumber;
        ViewModel.MethodName = MethodName;
        InstrumentName = InstrumentFilterModel.GetInstrumentName(InstrumentNumber);

        await ViewModel.LoadAsync();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= OnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string? GetConjugateLotNumber(CurveControlTestResult result)
    {
        if (result.Replicates?.First() == null)
        {
            return string.Empty;
        }

        return result.Replicates.First().ConjugateLotNumber;
    }
}