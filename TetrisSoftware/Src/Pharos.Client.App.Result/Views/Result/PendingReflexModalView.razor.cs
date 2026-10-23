using Microsoft.Extensions.Localization;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Common.Web.Components.Toaster;

namespace Pharos.Client.App.Result.Views.Result;

public partial class PendingReflexModalView
{
    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Parameter] public ICollection<PendingReflex> TestsSelected { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] public PendingReflexViewModel ViewModel { get; set; } = default!;
    [Inject] private IToasterService ToasterService { get; set; } = default!;

    private HashSet<PendingReflex> _selectedReflexTests = new();
    private List<PendingReflex> _displayedTests = new();
    private PhDataGrid<PendingReflex>? _dataGrid;

    public bool CanAddReflexTests => _displayedTests.Count > 0;
    public bool CanPerformActions => _selectedReflexTests.Count > 0;

    private bool _itemsAffected;

    private async Task CloseModal()
    {
        if (_itemsAffected)
        {
            await Modal.CloseAsync(ModalResult.Ok());
        }
        else
        {
            await Modal.CloseAsync(ModalResult.Cancel());
        }
    }

    private void OnSelectedItemsChanged(HashSet<PendingReflex> selectedItems)
    {
        _selectedReflexTests = selectedItems;
        StateHasChanged();
    }

    private async Task AddSelectedReflexTests()
    {
        if (_selectedReflexTests.Count == 0)
        {
            return;
        }

        await ViewModel.AddPendingReflexTests(_selectedReflexTests);

        var count = _selectedReflexTests.Count;

        // Remove processed items and force grid refresh
        RemoveProcessedItemsAndRefresh(_selectedReflexTests);

        // Show toaster when adding reflex test.
        var toasterText = string.Format(@Loc["AddingReflexTests_Toast"], count);
        ToasterService.ShowToast(ToastType.Information, toasterText);
    }

    private async Task DiscardSelectedReflexTests()
    {
        if (_selectedReflexTests.Count == 0)
        {
            return;
        }

        await ViewModel.DiscardPendingReflexTests(_selectedReflexTests);
        
        // Remove processed items and force grid refresh
        RemoveProcessedItemsAndRefresh(_selectedReflexTests);
    }

    private void RemoveProcessedItemsAndRefresh(HashSet<PendingReflex> processedItems)
    {
        // Get TestOrderIds of processed items for reliable comparison
        var processedReflexIds = processedItems.Select(x => x.Id).ToHashSet();

        // Create a completely new list without the processed items
        _displayedTests = _displayedTests
            .Where(item => !processedReflexIds.Contains(item.Id))
            .ToList();
        
        _selectedReflexTests.Clear();
        _dataGrid?.ClearSelectedItem();

        _itemsAffected = true;

        StateHasChanged();
    }

    protected override void OnParametersSet()
    {
        _displayedTests = TestsSelected.ToList();
    }
}