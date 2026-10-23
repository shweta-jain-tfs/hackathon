using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class ConnectionsView : IDisposable
{
    [Inject] private ConnectionsViewModel ViewModel { get; set; } = default!;

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