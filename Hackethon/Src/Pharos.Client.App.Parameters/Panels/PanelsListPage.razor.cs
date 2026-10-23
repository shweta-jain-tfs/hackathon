using Pharos.Client.App.Parameters.Panels.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.Models.Notifications;
using Pharos.Contracts.Security;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Panels;

public partial class PanelsListPage : IDisposable
{
    [Inject] public PanelsListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IParametersNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private HashSet<MethodPanel> _selectedItems = [];
    private PhDataGrid<MethodPanel>? _dataGrid;
    private bool IsEditingDisabled { get; set; } = true;

    private bool HasSelectedItems => _selectedItems.Count > 0;

    private List<BreadcrumbItem> Breadcrumbs =>
    [
        new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new BreadcrumbItem(Loc["TestPanelPage_Header"], PageRoute.Parameters.TestPanels)
    ];

    protected override void OnInitialized()
    {
        NotificationProvider.ParameterUpdateFinished += OnParameterUpdateFinished;
    }

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    private void OnParameterUpdateFinished(object? sender, bool e)
    {
        StateHasChanged();
    }

    private void SelectedItemsChanged(HashSet<MethodPanel> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private void AddPanel()
    {
        Navigation.NavigateTo(PageRoute.Parameters.TestPanelDetails.Default);
    }
    private async Task DeleteSelectedPanels()
    {
        if (_selectedItems.Count == 0)
        {
            return;
        }

        var modal = ModalService.ShowConfirm(
            Loc["TestPanelPage_DeleteDialogMessage"],
            Loc["TestPanelPage_DeleteDialogCancel"],
            Loc["TestPanelPage_DeleteDialogConfirm"],
            new ModalOptions { Title = Loc["TestPanelPage_DeleteDialogHeader"], ModalHeight = ModalSize.XSmall, ModalWidth = ModalSize.Small },
            true);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        var selectedIds = _selectedItems.Select(x => x.Id).ToArray();
        var isPanelsReferenced = await ViewModel.CheckPanelsReferenced(selectedIds);
        if (isPanelsReferenced)
        {
            modal = ModalService.ShowConfirm(
                Loc["TestPanelPage_DeleteReferencedDialogMessage"],
                Loc["TestPanelPage_DeleteReferencedDialogCancel"],
                Loc["TestPanelPage_DeleteReferencedDialogConfirm"],
                new ModalOptions { Title = Loc["TestPanelPage_DeleteReferencedDialogHeader"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Small },
                true);

            result = await modal.Result;
            if (result.Canceled)
            {
                return;
            }
        }

        await ViewModel.DeletePanels(selectedIds, isPanelsReferenced);

        _selectedItems.Clear();
        if (_dataGrid is not null)
        {
            await _dataGrid.ClearSelectedItem();
        }

        StateHasChanged();
    }

    public void Dispose()
    {
        NotificationProvider.ParameterUpdateFinished -= OnParameterUpdateFinished;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }
}
