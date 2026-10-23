using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.App.PageRouting;
using Pharos.Client.Common.App.State.CurrentInstrument;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Breadcrumb;

namespace Pharos.Client.App.InstrumentManagement.Views;

public partial class ConnectionsPage : IDisposable
{
    [Inject] private ConnectionsViewModel ViewModel { get; set; } = default!;
    [Inject] private ICurrentInstrumentViewModel InstrumentModel { get; set; } = null!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    private List<BreadcrumbItem> Breadcrumbs =>
    [
        new(InstrumentModel.Name, PageRoute.Dashboard),
        new(Loc["ConnectionsPage_Title"], "")
    ];

    private bool IsSingleLisConnection => ViewModel.LisConnections.Count == 1;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        await ViewModel.LoadAsync();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}