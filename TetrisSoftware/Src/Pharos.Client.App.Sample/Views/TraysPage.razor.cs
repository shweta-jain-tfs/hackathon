using Microsoft.Extensions.Localization;
using Pharos.Client.App.Sample.ViewModels;
using Pharos.Client.App.Sample.Views.TrayPanel;
using Pharos.Client.App.Sample.Views.Trays;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Models.DataGrid;

namespace Pharos.Client.App.Sample.Views;

public partial class TraysPage : IDisposable
{
    [Inject] private TraysViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService Modal { get; set; } = default!;

    private TrayPanelView? _trayPanelView;
    private string? _trayIdForPanel;
    private HashSet<SampleTray>? _selectedItems;

    private bool HasSelectedItems => _selectedItems is { Count: > 0 };
    private Dictionary<string, string>? _trayLocationFilterDict = new();
    private PhDataGrid<SampleTray> _phDataGrid = default!;
    private SelectionTypeEnum SelectionType => string.IsNullOrEmpty(_trayIdForPanel) ?
        SelectionTypeEnum.MultiSelectWithSelectAll :
        SelectionTypeEnum.Off;

    //Parameter that can be used to open a specific tray directly, used by request tube details tab
    [SupplyParameterFromQuery] public string? TrayId { get; set; }

    //Parameter that can be used to open a select a certain position, used by request tube details tab
    [SupplyParameterFromQuery] public int? SelectedPosition { get; set; }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();

        // _phDataGrid is null if exception occurs during OnInitializedAsync
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        _phDataGrid?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        _trayLocationFilterDict = await ViewModel.GetTrayLocationFilterAsync();
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(TrayId))
        {
            _trayIdForPanel = TrayId;
            TrayId = null;
        }
    }

    private async ValueTask<GridItemsProviderResult<SampleTray>> ProvideGridItems(GridItemsProviderRequest<SampleTray> request)
    {
        var result = await ViewModel.ProvideGridItems(request);
        ResetLegendSelection();
        return result;
    }

    private void SelectedItemsChanged(HashSet<SampleTray> selectedItems)
    {
        _selectedItems = selectedItems;
        ResetLegendSelection();
    }

    private async Task AddTrayButtonClicked()
    {
        var options = new ModalOptions
        {
            Title = Loc["TraysPage_AddDialog_Title"],
            ModalHeight = ModalSize.Small,
            ModalWidth = ModalSize.Small
        };

        var modal = Modal.Show<AddTrayView>(options);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            if (_selectedItems?.Count > 0)
            {
                _selectedItems.Clear();
                await _phDataGrid.ClearSelectedItem();
            }
        }

        ResetLegendSelection();
    }

    /// <summary>
    /// Handles the event when a tray ID is clicked. Prompts for unsaved changes if needed.
    /// </summary>
    private async Task TrayIdClicked(string trayId)
    {
        if (_trayPanelView is not null && !await _trayPanelView.IsUnsavedChangesHandledAsync())
        {
            return;
        }
        _trayIdForPanel = trayId;
        SelectedPosition = null;
        ResetLegendSelection();
    }

    private void TrayPanelCloseClicked()
    {
        _trayIdForPanel = null;
        _trayPanelView = null;
        SelectedPosition = null;
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        InvokeAsync(StateHasChanged);
        ResetLegendSelection();
    }

    private int SelectionCount()
    {
        return _selectedItems?.Count ?? 0;
    }

    private async Task ClearTraysButtonClicked()
    {
        var selectionCount = SelectionCount();
        var title = GetClearTrayModalTitle(selectionCount);
        var message = GetClearTrayModalMessage(selectionCount);
        var cancelButtonText = (string)Loc["TraysPage_ClearDialog_ButtonCancel"];
        var confirmButtonText = GetClearTrayModalConfirmButtonText(selectionCount);

        var modal = Modal.Show<ClearTrayView>(

           new ModalOptions
           {
               Title = title,
               ModalWidth = ModalSize.Small,
               ModalHeight = ModalSize.Small,
               Status = ModalStatus.Info
           },

           new ModalParameters
           {
                   { nameof(ClearTrayView.Message), message },
                   { nameof(ClearTrayView.CancelButtonText), cancelButtonText },
                   { nameof(ClearTrayView.ConfirmButtonText), confirmButtonText }
           }

        );

        var result = await modal.Result;

        if (!result.Canceled)
        {
            if ((_selectedItems?.Count ?? 0) == 0)
            {
                return;
            }
            else
            {
                var isCleared = result.Data;
                if (isCleared is not null)
                {
                    await ClearSelectedRows();
                }
            }
        }

        ResetLegendSelection();
    }

    private string GetClearTrayModalTitle(int selectionCount)
    {
        return selectionCount > 1
                 ? Loc["TraysPage_ClearDialog_Title_Plural"]
                 : Loc["TraysPage_ClearDialog_Title_Singular"];
    }

    private string GetClearTrayModalMessage(int selectionCount)
    {
        return selectionCount > 1
                ? Loc["TraysPage_ClearDialog_Body_Plural"]
                : Loc["TraysPage_ClearDialog_Body_Singular"];
    }

    private string GetClearTrayModalConfirmButtonText(int selectionCount)
    {
        return selectionCount > 1
                ? string.Format(Loc["TraysPage_ClearDialog_ButtonPrimary_Plural"], selectionCount)
                : string.Format(Loc["TraysPage_ClearDialog_ButtonPrimary_Singular"], selectionCount);
    }


    private async Task ClearSelectedRows()
    {
        var result = await ViewModel.ClearTraysAsync(GetSelectedTrayIds().ToArray());

        if (result.Success)
        {
            _selectedItems?.Clear();
            await _phDataGrid.ClearSelectedItem();
        }
        else
        {
            if (result.ClearedTrayIds.Count > 0)
            {
                _selectedItems?.RemoveWhere(x => result.ClearedTrayIds.Contains(x.Id));
                await _phDataGrid.ClearSelectedItem(_selectedItems?.ToList());
            }

            ShowClearTrayNotPossibleDialog();
        }

        StateHasChanged();
    }

    private void ShowClearTrayNotPossibleDialog()
    {
        var selectionCount = SelectionCount();
        var title = GetClearTrayNotPossibleModalTitle(selectionCount);
        var message = GetClearTrayNotPossibleModalMessage(selectionCount);
        var closeButtonText = (string)Loc["TraysPage_ClearNotPossibleDialog_ButtonClose"];

        Modal.Show<ClearTrayNotPossibleView>(

             new ModalOptions
             {
                 Title = title,
                 ModalWidth = ModalSize.Small,
                 ModalHeight = ModalSize.Small,
                 Status = ModalStatus.Warning
             },

             new ModalParameters
             {
                 { nameof(ClearTrayNotPossibleView.Message), message },
                 { nameof(ClearTrayNotPossibleView.CloseButtonText), closeButtonText }
             }

        );
    }

    private string GetClearTrayNotPossibleModalTitle(int selectionCount)
    {
        return selectionCount > 1
                 ? Loc["TraysPage_ClearNotPossibleDialog_Title_Plural"]
                 : Loc["TraysPage_ClearNotPossibleDialog_Title_Singular"];
    }

    private string GetClearTrayNotPossibleModalMessage(int selectionCount)
    {
        return selectionCount > 1
                 ? Loc["TraysPage_ClearNotPossibleDialog_Body_Plural"]
                 : Loc["TraysPage_ClearNotPossibleDialog_Body_Singular"];
    }

    private async Task DeleteTraysButtonClicked()
    {
        var selectionCount = SelectionCount();
        var title = GetDeleteTrayModalTitle(selectionCount);
        var message = GetDeleteTrayModalMessage(selectionCount);
        var cancelButtonText = (string)Loc["TraysPage_DeleteDialog_ButtonCancel"];
        var confirmButtonText = GetDeleteTrayModalConfirmButtonText(selectionCount);


        var modal = Modal.Show<DeleteTrayView>(

               new ModalOptions
               {
                   Title = title,
                   ModalWidth = ModalSize.Small,
                   ModalHeight = ModalSize.Small,
                   Status = ModalStatus.Info
               },

               new ModalParameters
               {
                   { nameof(DeleteTrayView.Message), message },
                   { nameof(DeleteTrayView.CancelButtonText), cancelButtonText },
                   { nameof(DeleteTrayView.ConfirmButtonText), confirmButtonText }
               }

            );

        var result = await modal.Result;

        if (!result.Canceled)
        {
            if ((_selectedItems?.Count ?? 0) == 0)
            {
                return;
            }
            else
            {
                var isDeleted = result.Data;
                if (isDeleted is not null)
                {
                    await DeleteSelectedRows();
                }
            }
        }

        ResetLegendSelection();
    }

    private string GetDeleteTrayModalTitle(int selectionCount)
    {
        return selectionCount > 1
                 ? Loc["TraysPage_DeleteDialog_Title_Plural"]
                 : Loc["TraysPage_DeleteDialog_Title_Singular"];
    }

    private string GetDeleteTrayModalMessage(int selectionCount)
    {
        return selectionCount > 1
                ? Loc["TraysPage_DeleteDialog_Body_Plural"]
                : Loc["TraysPage_DeleteDialog_Body_Singular"];
    }

    private string GetDeleteTrayModalConfirmButtonText(int selectionCount)
    {
        return selectionCount > 1
                ? string.Format(Loc["TraysPage_DeleteDialog_ButtonPrimary_Plural"], selectionCount)
                : string.Format(Loc["TraysPage_DeleteDialog_ButtonPrimary_Singular"], selectionCount);
    }


    private async Task DeleteSelectedRows()
    {
        var result = await ViewModel.DeleteTraysAsync(GetSelectedTrayIds().ToArray());

        if (result.Success)
        {
            _selectedItems?.Clear();
            await _phDataGrid.ClearSelectedItem();
        }
        else
        {
            if (result.DeletedTrayIds.Count > 0)
            {
                _selectedItems?.RemoveWhere(x => result.DeletedTrayIds.Contains(x.Id));
                await _phDataGrid.ClearSelectedItem(_selectedItems?.ToList());
            }

            ShowDeleteTrayNotPossibleDialog();
        }

        StateHasChanged();
    }

    private void ShowDeleteTrayNotPossibleDialog()
    {
        var selectionCount = SelectionCount();
        var title = GetDeleteTrayNotPossibleModalTitle(selectionCount);
        var message = GetDeleteTrayNotPossibleModalMessage(selectionCount);
        var closeButtonText = (string)Loc["TraysPage_DeleteNotPossibleDialog_ButtonClose"];

        Modal.Show<DeleteTrayNotPossibleView>(

             new ModalOptions
             {
                 Title = title,
                 ModalWidth = ModalSize.Small,
                 ModalHeight = ModalSize.Small,
                 Status = ModalStatus.Warning
             },

             new ModalParameters
             {
                 { nameof(ClearTrayNotPossibleView.Message), message },
                 { nameof(ClearTrayNotPossibleView.CloseButtonText), closeButtonText }
             }


        );
    }

    private string GetDeleteTrayNotPossibleModalTitle(int selectionCount)
    {
        return selectionCount > 1
                 ? Loc["TraysPage_DeleteNotPossibleDialog_Title_Plural"]
                 : Loc["TraysPage_DeleteNotPossibleDialog_Title_Singular"];
    }

    private string GetDeleteTrayNotPossibleModalMessage(int selectionCount)
    {
        return selectionCount > 1
                 ? Loc["TraysPage_DeleteNotPossibleDialog_Body_Plural"]
                 : Loc["TraysPage_DeleteNotPossibleDialog_Body_Singular"];
    }

    private List<string> GetSelectedTrayIds()
    {
        var ids = new List<string>();

        if (_selectedItems?.Count > 0)
        {
            ids = _selectedItems!.Select(x => x.Id).ToList();
        }

        return ids;
    }

    private void ResetLegendSelection()
    {
        _trayPanelView?.ResetLegendSelection();
    }
}