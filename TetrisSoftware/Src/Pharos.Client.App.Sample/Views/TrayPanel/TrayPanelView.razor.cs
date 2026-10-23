using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.Models;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.Drawer;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Utilities;
using SampleType = Pharos.Common.Web.Components.Drawer.SampleType;

namespace Pharos.Client.App.Sample.Views.TrayPanel;

public partial class TrayPanelView : ComponentBase, IDisposable
{
    private const int _maxTubeBarcodeLength = SampleConstants.SampleIdMaxLength;

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject] private TrayPanelViewModel ViewModel { get; set; } = default!;

    [Inject] private IModalService Modal { get; set; } = default!;

    [Parameter] public string? TrayId { get; set; }
    [Parameter] public int? InitialSelectedPosition { get; set; }

    [Parameter] public EventCallback OnCloseClick { get; set; }

    [Parameter] public bool HasLegendView { get; set; } = false;

    [CascadingParameter(Name = "SampleDrawerOnResetLegendSelectionRequested")]
    public Action? SampleDrawerOnResetLegendSelectionRequested { get; set; }

    public void Dispose()
    {
        ViewModel.DrawerContentsReceived -= OnDrawerContentsReceived;
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.LegendViewModel.PropertyChanged -= OnLegendPropertyChanged;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override void OnInitialized()
    {
        ViewModel.DrawerContentsReceived += OnDrawerContentsReceived;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.LegendViewModel.PropertyChanged += OnLegendPropertyChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (TrayId == ViewModel.Entity.Id)
        {
            return;
        }

        if (!ViewModel.IsBusy && !string.IsNullOrEmpty(TrayId))
        {
            ViewModel.IsSampleTubeSelected = false;
            ViewModel.SetLegendVisibility(HasLegendView);

            try
            {
                await ViewModel.LoadAsync(TrayId);

                if (InitialSelectedPosition.HasValue)
                {
                    var tube = ViewModel.GetSampleTubes().FirstOrDefault(x => x.Position == InitialSelectedPosition.Value);
                    if (tube != null)
                    {
                        await HandleSampleTubeSelected(tube);
                    }

                    InitialSelectedPosition = null;
                }
            }
            catch (ApiException e)
            {
                // If the tray is not found (404), close the panel
                if (e.StatusCode == 404)
                {
                    await CloseButtonClicked();
                }
            }
        }
    }

    public void ResetLegendSelection()
    {
        ViewModel.LegendViewModel.OnResetLegendSelectionRequested();
        SampleDrawerOnResetLegendSelectionRequested?.Invoke();
    }

    private async Task CloseButtonClicked()
    {
        await OnCloseClick.InvokeAsync();
        SampleDrawerOnResetLegendSelectionRequested?.Invoke();
    }

    private string GetTubeBarcodeLabel()
    {
        if (ViewModel.IsEditMode)
        {
            return Loc["TrayPanel_InputLabel_TubeBarcode"];
        }

        return ViewModel.SelectedSampleType switch
        {
            SampleType.QualityControl => Loc["TrayPanel_InputLabel_QCID"],
            _ => Loc["TrayPanel_InputLabel_SampleID"]
        };
    }

    private async void OnDrawerContentsReceived(object? sender, EventArgs e)
    {
        await ViewModel.DiscardChangesAsync();
        Modal.Show<TrayPanelEditNotPossibleView>(new ModalOptions { Title = Loc["TrayPanel_EditNotPossibleDialog_Title"] });
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    private void OnLegendPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.LegendViewModel.LegendSelectionResult))
        {
            StateHasChanged();
        }
    }

    private void HandleSampleIdInputFocus()
    {
        ResetLegendSelection();
    }

    private void HandleHighPriorityCheckChanged()
    {
        ResetLegendSelection();
    }

    private void HandleEditAction()
    {
        if (!ViewModel.IsTrayEditable)
        {
            Modal.Show<OpenDrawerToEditTrayView>(new ModalOptions
            {
                Title = Loc["TrayPanel_OpenDrawerToEditTrayDialog_Title"],
                ModalWidth = ModalSize.Small,
                ModalHeight = ModalSize.Small,
                Status = ModalStatus.Info
            });
        }
        else
        {
            ResetLegendSelection();
            ViewModel.EditTrayPanelButtonClicked();
        }
    }

    private async Task HandleDiscardAction()
    {
        ResetLegendSelection();
        await ViewModel.LoadAsync(TrayId!);
    }

    private async Task HandleSaveAction()
    {
        ResetLegendSelection();
        await ViewModel.SaveAsync();
    }

    private async Task<bool> HandleSampleTubeSelected(SampleTube tube)
    {
        if (await IsUnsavedChangesHandledAsync())
        {
            await ViewModel.SampleTubeSelected(tube);
        }
        return ViewModel.IsSelectionChanged;
    }

    public async Task<bool> IsUnsavedChangesHandledAsync()
    {
        if (ViewModel.IsModified)
        {
            var modal = Modal.Show<UnsavedChangesView>(new ModalOptions { Title = Loc["TrayPanel_UnsavedChangesDialog_Title"] });
            var result = await modal.Result;

            if (result.Canceled)
            {
                ViewModel.IsSelectionChanged = false;
                return false;
            }

            if (result.Data is bool saveClicked)
            {
                if (saveClicked)
                {
                    await HandleSaveAction();
                    await ViewModel.LoadAsync(TrayId!);
                }
                else
                {
                    await HandleDiscardAction();
                }
            }
        }
        return true;
    }

    private string TrayDetailCssClass =>
         new CssBuilder("ph-tray-detail")
        .AddClass("ph-tray-detail--expanded", HasLegendView)
        .Build();
}