using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Parameters.Reflex;

public partial class ReflexDefinitionListPage : IDisposable
{
    private PhDataGrid<ReflexDefinitionSummary>? _dataGrid;
    private HashSet<ReflexDefinitionSummary>? _selectedItems;
    private UiSettings? _uiSettings;

    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private ReflexDefinitionListPageViewModel PageViewModel { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;
    private bool HasViewPersonalInfoPermission { get; set; }
    private bool CanEditReflex { get; set; }

    private bool DisplayRequestor => (_uiSettings?.RequestSettings.IsRequestorInformationEnabled ?? false) && HasViewPersonalInfoPermission;

    private bool HasSelectedItems => _selectedItems is { Count: > 0 };

    private List<BreadcrumbItem> Breadcrumbs => [
        new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
        new BreadcrumbItem(Loc["NavMenu_Reflex"], PageRoute.Parameters.Reflex),
    ];

    public void Dispose()
    {
        PageViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        PageViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task OnInitializedAsync()
    {
        PageViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;

        _uiSettings = await UiSettingsService.GetSettingsAsync();
        HasViewPersonalInfoPermission = await AuthorizationService.IsAuthorized(Permissions.ViewPersonalAndHealthData);
        CanEditReflex = await AuthorizationService.IsAuthorized(Permissions.EditTestSettings);
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    private async Task DeleteSelectedRows()
    {
        if (!CanEditReflex || _selectedItems == null || _selectedItems.Count == 0)
        {
            return;
        }

        var modal = ModalService.ShowConfirm(Loc["ReflexListPage_DeleteConfirmation_Message"], Loc["ReflexListPage_DeleteConfirmation_Cancel"], Loc["ReflexListPage_DeleteConfirmation_Ok"], new ModalOptions
        {
            Title = Loc["ReflexListPage_DeleteConfirmation_Title"],
            Status = ModalStatus.Info,
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small
        }, true);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        await PageViewModel.DeleteReflexDefinition(_selectedItems.Select(x => x.Id).ToArray());
        _dataGrid?.ClearSelectedItem();
        _selectedItems.Clear();
    }

    private void SelectedItemsChanged(HashSet<ReflexDefinitionSummary> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private void AddReflexDefinition()
    {
        if (!CanEditReflex)
        {
            return;
        }

        Navigation.NavigateTo(PageRoute.Parameters.ReflexDefinitionDetails.Default);
    }

    private async Task SetIsActive(int reflexId, bool isActive)
    {
        if (!CanEditReflex)
        {
            return;
        }

        await PageViewModel.SetActiveReflexDefinition(reflexId, isActive);
    }
}