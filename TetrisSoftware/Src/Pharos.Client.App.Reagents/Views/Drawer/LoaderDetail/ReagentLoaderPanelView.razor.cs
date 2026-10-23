using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Reagents.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Client.Common.Models;

namespace Pharos.Client.App.Reagents.Views.Drawer.LoaderDetail;

public partial class ReagentLoaderPanelView : IDisposable
{
    [Inject]
    public IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Parameter]
    public ReagentLoaderPanelViewModel Loader { get; set; } = default!;

    [Parameter]
    public EventCallback OnCloseClick { get; set; }

    public bool IsCellContentNull => Loader?.ReagentContentDetail == null;
    public string DisplayLabel => Loader.SelectedReagentType ? Loc["ReagentPage_Test"] : Loc["ReagentPage_Method"];

    protected override void OnInitialized()
    {
        Loader.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Loader.SelectedReagentLoader))
        {
            ClosePanelIfLoaderEmpty();
        }
        StateHasChanged();
    }

    public void Dispose()
    {
        if (Loader.SelectedReagentLoader != null)
        {
            Loader.ResetLoader();
        }
        Loader.PropertyChanged -= OnViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }

    private async void CloseButtonClicked()
    {
        Loader.ClearSelection();
        await OnCloseClick.InvokeAsync();
    }

    private void SelectedCellContent(CompartmentCellContent cellContent, CompartmentReagentType reagentType)
    {
        if (!cellContent.IsEmpty)
        {
            Loader.SelectedReagentType = reagentType == CompartmentReagentType.Carrier;
            Loader.SelectedCellContent = cellContent;
            Loader.InvokeClearLegendSelction();
        }
    }

    private void ClosePanelIfLoaderEmpty()
    {
        if (Loader.SelectedReagentLoader != null && Loader.SelectedReagentLoader.IsEmpty)
        {
            CloseButtonClicked();
        }
    }
}
