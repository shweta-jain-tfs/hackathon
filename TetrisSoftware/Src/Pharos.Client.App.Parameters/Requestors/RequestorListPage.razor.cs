using Pharos.Client.App.Parameters.Requestors.ViewModels;
using Pharos.Client.App.Parameters.Requestors.Views;
using Pharos.Client.Common.App.Services;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Requestors;

public partial class RequestorListPage : IDisposable
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private RequestorListViewModel ViewModel { get; set; } = default!;
    [Inject] private UiSettingsService UiSettingsService { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private UiSettings? _uiSettings;
    private HashSet<RequestorSummary>? _selectedItems;
    private PhDataGrid<RequestorSummary>? _dataGrid;

    private bool HasSelectedItems => _selectedItems is { Count: > 0 };

    private List<BreadcrumbItem> Breadcrumbs => [
            new (Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new (Loc["NavMenu_Requestors"], PageRoute.Parameters.Requestors),
        ];

    protected override void OnInitialized()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
    }

    protected override async Task OnInitializedAsync()
    {
        _uiSettings = await UiSettingsService.GetSettingsAsync();
    }

    private bool ShowReflexColumn => _uiSettings != null && _uiSettings.RequestSettings.ReflexTestingEnabled;

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        ViewModel.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task DeleteRequestorsOrOpenDialog()
    {
        // Confirmation dialog.
        var parameters = new ModalParameters
        {
            {nameof(RequestorListDeleteView.PrimaryButtonText), GetButtonText()}
        };

        var modal = ModalService.Show<RequestorListDeleteView>(new ModalOptions { Title = @Loc["RequestorListPage_DeleteDialogHeader"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Medium }, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            var deleteResponse = await DeleteSelectedRequestors();

            if (deleteResponse.Skipped > 0)
            {
                var infoParameters = new ModalParameters
                {
                    { nameof(InformationModal.Skipped), deleteResponse.Skipped }
                };

                var infoModal = ModalService.Show<InformationModal>(new ModalOptions { Title = @Loc["RequestorListPage_DeleteDialogHeader"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Medium }, infoParameters);
                await infoModal.Result;
            }
        }
    }

    private string GetButtonText()
    {
        var selectionCount = _selectedItems?.Count ?? 0;
        if (selectionCount > 0)
        {
            // Use singular for 1 item, plural for multiple items
            return string.Format(selectionCount == 1 ? Loc["RequestorListPage_DeleteDialogButtonPrimary_Single"] : Loc["RequestorListPage_DeleteDialogButtonPrimary_Multiple"], selectionCount);
        }

        return string.Empty;
    }

    private async Task<DeleteResponse> DeleteSelectedRequestors()
    {
        if (_selectedItems != null)
        {
            var deleteResponse = await ViewModel.DeleteRequestors(_selectedItems!.Select(x => x.Id).ToArray());
            _selectedItems!.RemoveWhere(x => !deleteResponse.SkippedIds.Contains(x.Id));
            _dataGrid?.ClearSelectedItem(_selectedItems.ToList());
            return deleteResponse;
        }

        return new DeleteResponse();
    }

    private void SelectedItemsChanged(HashSet<RequestorSummary> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private void AddRequestor()
    {
        Navigation.NavigateTo($"{PageRoute.Parameters.RequestorDetails.Default}");
    }
}