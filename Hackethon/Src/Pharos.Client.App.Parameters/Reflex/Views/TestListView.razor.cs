using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class TestListView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Parameter] public string? DefinitionName { get; set; }
    [Parameter, EditorRequired] public ReflexDefinitionDetailsViewModel ViewModel { get; set; } = default!;

    private PhDataGrid<ReflexTestViewModel>? _dataGrid;
    private HashSet<ReflexTestViewModel>? _selectedItems = new();

    private bool HasAnyItemsSelected => _selectedItems is { Count: >= 1 };

    private void AddIndexToTestSortOrder(int addIndexToTestSortOrder)
    {
        if (_selectedItems == null)
        {
            return;
        }

        var selectedRow = _selectedItems.First();
        var oldTestSortOrderForSelectedRow = selectedRow.TestSortOrder;
        var newTestSortOrderForSelectedRow = selectedRow.TestSortOrder + addIndexToTestSortOrder;

        var rowToSwapWith = ViewModel.ReflexTestViewModels.FirstOrDefault(x => x.TestSortOrder == newTestSortOrderForSelectedRow);
        if (rowToSwapWith != null)
        {
            // Set position for row to swap with.
            rowToSwapWith.TestSortOrder = oldTestSortOrderForSelectedRow;
        }

        // Set new position for selected row.
        selectedRow.TestSortOrder = newTestSortOrderForSelectedRow;

        StateHasChanged();
    }

    public void DiscardChanges()
    {
        _selectedItems?.Clear();
        _dataGrid?.ClearSelectedItem();
    }

    private bool IsEmptyDefinitionName()
    {
        return string.IsNullOrEmpty(DefinitionName);
    }

    private bool MoveTestOrderDownDisabled()
    {
        if (!ViewModel.CanEdit || _selectedItems is not { Count: 1 } || ViewModel.ReflexTestViewModels.Count == 0)
        {
            return true;
        }

        var selectedRow = _selectedItems.First();
        return selectedRow.TestSortOrder == ViewModel.ReflexTestViewModels.Max(x => x.TestSortOrder);
    }

    private bool MoveTestOrderUpDisabled()
    {
        if (!ViewModel.CanEdit || _selectedItems is not { Count: 1 } || ViewModel.ReflexTestViewModels.Count == 0)
        {
            return true;
        }

        var selectedRow = _selectedItems.First();
        return selectedRow.TestSortOrder == ViewModel.ReflexTestViewModels.Min(x => x.TestSortOrder);
    }

    private void SelectedItemsChanged(HashSet<ReflexTestViewModel> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    public async Task TestsAddViewOpen()
    {
        if (!ViewModel.CanEdit)
        {
            return;
        }

        var parameters = new ModalParameters
        {
            { nameof(TestAddView.CurrentTestOrders), ViewModel.ReflexTestViewModels.Select(x => new ReflexTest
            {
                TestName = x.TestName,
                TestFullName = x.TestFullName,
                MethodName = x.MethodName,
                MethodFullName = x.MethodFullName,
                NumberOfReplicates = x.NumberOfReplicates,
                InstrumentDilutionFactor = x.InstrumentDilutionFactor,
            }) }
        };

        var model = ModalService.Show<TestAddView>(new ModalOptions { Title = Loc["TestAddView_Title"], ModalHeight = ModalSize.XLarge, ModalWidth = ModalSize.Large }, parameters);
        var result = await model.Result;

        if (!result.Canceled)
        {
            var data = (HashSet<MethodTest>?)result.Data;
            ViewModel.AddMethodTests(data);
        }
    }

    private async Task TestsDeleteSelectedRows()
    {
        if (!ViewModel.CanEdit || _selectedItems == null || _selectedItems.Count == 0)
        {
            return;
        }

        var modal = ModalService.ShowConfirm(Loc["ReflexDetailPage_DeleteConfirmation_Message"], Loc["ReflexDetailPage_DeleteConfirmation_Cancel"], Loc["ReflexDetailPage_DeleteConfirmation_Ok"], new ModalOptions
        {
            Title = Loc["ReflexDetailPage_DeleteConfirmation_Title"],
            Status = ModalStatus.Info,
            ModalHeight = ModalSize.XSmall,
            ModalWidth = ModalSize.Small
        }, true);

        var result = await modal.Result;
        if (result.Canceled)
        {
            return;
        }

        ViewModel.DeleteTest(_selectedItems);
        _selectedItems.Clear();
        _dataGrid?.ClearSelectedItem();

        SelectedItemsChanged(_selectedItems);
    }

    private async Task TestsEditViewOpen(ReflexTestViewModel testViewModel)
    {
        var test = new ReflexTest
        {
            TestName = testViewModel.TestName,
            MethodName = testViewModel.MethodName,
            TestFullName = testViewModel.TestFullName,
            MethodFullName = testViewModel.MethodFullName,
            NumberOfReplicates = testViewModel.NumberOfReplicates,
            InstrumentDilutionFactor = testViewModel.InstrumentDilutionFactor
        };

        var parameters = new ModalParameters
        {
            { nameof(TestEditView.ReflexTest), test },
            { nameof(TestEditView.CanEdit), ViewModel.CanEdit }
        };

        var modal = ModalService.Show<TestEditView>(new ModalOptions { Title = Loc["TestEditView_Title"], ModalHeight = ModalSize.Large, ModalWidth = ModalSize.Medium }, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            testViewModel.NumberOfReplicates = test.NumberOfReplicates;
            testViewModel.InstrumentDilutionFactor = test.InstrumentDilutionFactor;
        }
    }
}