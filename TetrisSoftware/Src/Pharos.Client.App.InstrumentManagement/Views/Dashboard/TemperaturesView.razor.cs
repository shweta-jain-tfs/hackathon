using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;
using Pharos.Client.Common.Localization;

namespace Pharos.Client.App.InstrumentManagement.Views.Dashboard;

public partial class TemperaturesView : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private TemperaturesViewModel ViewModel { get; set; } = default!;

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