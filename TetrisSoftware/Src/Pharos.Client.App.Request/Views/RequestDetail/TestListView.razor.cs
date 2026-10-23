using Pharos.Client.App.Request.ViewModels;
using Pharos.Client.App.Request.Views.RequestList;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;
using Pharos.Contracts.Security;

namespace Pharos.Client.App.Request.Views.RequestDetail;

public partial class TestListView
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] public IAuthorizationService AuthorizationService { get; set; } = null!;
    [Parameter, EditorRequired] public RequestDetailsViewModel ViewModel { get; set; } = default!;

    private PhDataGrid<TestOrderViewModel>? _dataGrid;
    private HashSet<TestOrderViewModel>? _selectedItems = [];
    private static readonly List<TestOrderState> _statesThatCanBeHeldOrReleased = [TestOrderState.NotStarted, TestOrderState.Scheduled, TestOrderState.UnableToStart];

    private bool HasManageTestOrdersPermission { get; set; }

    private bool HasAnyItemsSelected => _selectedItems is { Count: >= 1 };

    public bool CanHoldTest => _selectedItems != null && _selectedItems.Any(x => x.IsActive && _statesThatCanBeHeldOrReleased.Contains(x.State));

    public bool CanReleaseTest => _selectedItems != null && _selectedItems.Any(x => !x.IsActive && _statesThatCanBeHeldOrReleased.Contains(x.State));

    // Property to check if reflex testing is enabled, defaulting to false until settings are loaded
    private bool GetIsReflexTestingEnabled()
    {
        return ViewModel.UiSettings.RequestSettings?.ReflexTestingEnabled ?? true;
    }

    private bool ShouldShowConcentrationColumn()
    {
        return ViewModel.TestOrders?.Any(t => t.ReportConcentration) ?? false;
    }

    private bool ShouldShowClassColumn()
    {
        return ViewModel.TestOrders?.Any(t => t.ReportClass) ?? false;
    }

    private bool ShouldShowCutOffColumn()
    {
        return ViewModel.TestOrders?.Any(t => t.ReportCutOff) ?? false;
    }

    private bool ShouldShowQuotientColumn()
    {
        return ViewModel.TestOrders?.Any(t => t.ReportQuotient) ?? false;
    }

    protected override async Task OnInitializedAsync()
    {
        HasManageTestOrdersPermission = await AuthorizationService.IsAuthorized(Permissions.ManageTestOrders);
    }

    private async Task AddIndexToTestSortOrder(int addIndexToTestSortOrder)
    {
        if (_selectedItems == null)
        {
            return;
        }

        var selectedRow = _selectedItems.First();
        var oldTestSortOrderForSelectedRow = selectedRow.TestSortOrder;
        var newTestSortOrderForSelectedRow = selectedRow.TestSortOrder + addIndexToTestSortOrder;

        var rowToSwapWith = ViewModel.TestOrders.FirstOrDefault(x => x.TestSortOrder == newTestSortOrderForSelectedRow);
        if (rowToSwapWith != null)
        {
            // Set position for row to swap with.
            rowToSwapWith.TestSortOrder = oldTestSortOrderForSelectedRow;
        }

        // Set new position for selected row.
        selectedRow.TestSortOrder = newTestSortOrderForSelectedRow;

        await ViewModel.ReCalculateVolumeNeeded();
    }

    public void DiscardChanges()
    {
        _selectedItems?.Clear();
        _dataGrid?.ClearSelectedItem();
    }

    private bool MoveTestOrderDownDisabled()
    {
        if (_selectedItems is not { Count: 1 } || ViewModel.TestOrders.Count == 0)
        {
            return true;
        }

        var selectedRow = _selectedItems.First();
        return selectedRow.TestSortOrder == ViewModel.TestOrders.Max(x => x.TestSortOrder);
    }

    private bool MoveTestOrderUpDisabled()
    {
        if (_selectedItems is not { Count: 1 } || ViewModel.TestOrders.Count == 0)
        {
            return true;
        }

        var selectedRow = _selectedItems.First();
        return selectedRow.TestSortOrder == ViewModel.TestOrders.Min(x => x.TestSortOrder);
    }

    private bool ResetButtonDisabled()
    {
        // Disable if no items are selected or all selected items are already NotStarted
        return _selectedItems is not { Count: > 0 } ||
               _selectedItems.All(x => x.State == TestOrderState.NotStarted);
    }

    private void SelectedItemsChanged(HashSet<TestOrderViewModel> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    public async Task TestsAddViewOpen()
    {
        var parameters = new ModalParameters
        {
            { nameof(TestAddView.CurrentTestOrders), ViewModel.TestOrders }
        };

        var model = ModalService.Show<TestAddView>(new ModalOptions { Title = Loc["RequestDetail_TestListView_TestAddView_Title"], ModalHeight = ModalSize.XLarge, ModalWidth = ModalSize.Large }, parameters);
        var result = await model.Result;

        if (!result.Canceled)
        {
            var data = (HashSet<MethodTest>?)result.Data;
            SetPreDilution(data);
            var showMessage = await ViewModel.AddMethodTestsAsync(data);

            if (showMessage)
            {
                ShowPreDilutionFactorMismatchModal();
            }
        }
    }

    private void ShowPreDilutionFactorMismatchModal()
    {
        ModalService.ShowInformation(Loc["TestListView_PreDilutionFactorMismatch_Text"], Loc["TestListView_PreDilutionFactorMismatch_Ok"], new ModalOptions { Status = ModalStatus.Info, Title = Loc["TestListView_PreDilutionFactorMismatch_Title"] }, true);
    }

    private void SetPreDilution(HashSet<MethodTest>? tests)
    {
        if (ViewModel.TestOrders.Count > 0)
        {
            return;
        }

        var firstTest = tests?.FirstOrDefault();

        if (firstTest == null)
        {
            return;
        }

        SetPreDilution(firstTest);
    }

    private void SetPreDilution(MethodTest test)
    {
        if (!test.UsesInstrumentDilution)
        {
            ViewModel.PreDilutionFactor = test.DilutionFactor;
        }
    }

    private async Task TestsDeleteButtonClicked()
    {
        if (_selectedItems == null)
        {
            return;
        }

        if (_selectedItems.All(x => x.State == TestOrderState.NotStarted))
        {
            await TestsDeleteSelectedRows();
        }
        else
        {
            var parameters = new ModalParameters
            {
                { nameof(TestDeleteView.SelectedRequestTests), _selectedItems }
            };

            var modal = ModalService.Show<TestDeleteView>(new ModalOptions { Title = Loc["TestListView_DeleteDialogHeader"], ModalHeight = ModalSize.Medium, ModalWidth = ModalSize.Medium }, parameters);

            var result = await modal.Result;

            if (!result.Canceled)
            {
                await TestsDeleteSelectedRows();
            }
        }
    }

    private async Task TestsDeleteSelectedRows()
    {
        if (_selectedItems == null)
        {
            return;
        }

        await ViewModel.DeleteTest(_selectedItems);
        _selectedItems.Clear();
        _dataGrid?.ClearSelectedItem();
    }

    private async Task TestsResetButtonClicked()
    {
        if (_selectedItems == null || _selectedItems.Count == 0)
        {
            return;
        }

        // Client-side reset: marks tests for reset and clears result data for immediate UI feedback.
        // The server will delete and recreate these tests with new UIDs while preserving test properties and reflex definitions.

        var testsToReset = _selectedItems
            .Where(x => x.State
                is TestOrderState.Aborted
                or TestOrderState.Pipetted
                or TestOrderState.Scheduled
                or TestOrderState.Started
                or TestOrderState.UnableToStart
                )
            .ToList();

        var parameters = new ModalParameters
        {
            {nameof(RequestDetailResetView.TestsResetCount), testsToReset.Count},
            {nameof(RequestDetailResetView.TestsSelectedCount), _selectedItems.Count}
        };

        var modal = ModalService.Show<RequestDetailResetView>(
            new ModalOptions
            {
                Title = Loc["RequestDetailPage_ResetDialogHeader"],
                Status = ModalStatus.Info,
                ModalHeight = ModalSize.Medium,
                ModalWidth = ModalSize.Small,
            }, parameters);

        var result = await modal.Result;

        if (result is { Canceled: false })
        {
            foreach (var test in testsToReset)
            {
                test.ResetForClient();
            }

            // Clear selection after successful reset
            _selectedItems.Clear();
            _dataGrid?.ClearSelectedItem();

            await ViewModel.ReCalculateVolumeNeeded();

            StateHasChanged();
        }
    }

    public async Task PanelsAddViewOpen()
    {
        var modal = ModalService.Show<AddTestPanelsView>(new ModalOptions { Title = Loc["RequestDetailsPage_AddTestPanelsView_Title"], ModalHeight = ModalSize.XLarge, ModalWidth = ModalSize.Medium });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            var data = (HashSet<Panel>?)result.Data;
            SetPreDilution(data);
            var showMessage = await ViewModel.AddPanelTests(data);

            if (showMessage)
            {
                ShowPreDilutionFactorMismatchModal();
            }
        }
    }

    private void SetPreDilution(HashSet<Panel>? panels)
    {
        if (ViewModel.TestOrders.Count > 0)
        {
            return;
        }

        var firstTest = panels?.FirstOrDefault()?.PanelTests.FirstOrDefault();

        if (firstTest == null)
        {
            return;
        }

        SetPreDilution(firstTest.MethodTest);
    }

    private async Task TestsEditViewOpen(TestOrderViewModel requestTest)
    {
        var model = new RequestTest
        {
            TestName = requestTest.TestName,
            LabTestName = requestTest.LabTestName,
            FullTestName = requestTest.FullTestName,
            MethodName = requestTest.MethodName,
            FullMethodName = requestTest.FullMethodName,
            InstrumentDilutionFactor = requestTest.InstrumentDilutionFactor,
            NumberOfReplicates = requestTest.NumberOfReplicates,
            State = requestTest.State,
            IsActive = requestTest.IsActive
        };
        var copy = new TestOrderViewModel(model, Loc);

        var isEditMode = requestTest.State is TestOrderState.NotStarted
                                           or TestOrderState.Scheduled
                                           or TestOrderState.UnableToStart
                         && HasManageTestOrdersPermission;

        var parameters = new ModalParameters
        {
            { nameof(TestEditView.RequestTest), copy },
            { nameof(TestEditView.IsEditMode), isEditMode },
            { nameof(TestEditView.PreDilutionFactor), ViewModel.PreDilutionFactor }
        };

        var modalTitle = @Loc["TestEditView_ModalTitle"];
        var modal = ModalService.Show<TestEditView>(new ModalOptions { Title = modalTitle, ModalHeight = ModalSize.Large, ModalWidth = ModalSize.Large }, parameters);

        var result = await modal.Result;

        if (!result.Canceled && isEditMode)
        {
            requestTest.InstrumentDilutionFactor = copy.InstrumentDilutionFactor;
            requestTest.NumberOfReplicates = copy.NumberOfReplicates;

            await ViewModel.ReCalculateVolumeNeeded();
        }

        copy.Dispose();
    }

    private async Task ReflexViewOpen(TestOrderState testOrderState, RequestReflexDefinition reflexDefinition)
    {
        var parameters = new ModalParameters
        {
            { nameof(ReflexDeleteView.ReflexCopyDetails), reflexDefinition },
            { nameof(ReflexDeleteView.TestOrderState), testOrderState },
            {nameof(ReflexDeleteView.DataGridTitleText), string.Format(@Loc["ReflexDetailPage_Title"], reflexDefinition.ReflexTests.Count)}
        };

        var modal = ModalService.Show<ReflexDeleteView>(new ModalOptions { Title = @Loc["ReflexDetailPage_ModalTitle"], ModalHeight = ModalSize.Large, ModalWidth = ModalSize.Medium }, parameters);

        var result = await modal.Result;

        if (!result.Canceled)
        {
            var data = (RequestReflexDefinition?)result.Data;
            if (data == null)
            {
                return;
            }

            await DeleteReflexOrOpenDialog(data);
        }
    }

    private async Task DeleteReflexOrOpenDialog(RequestReflexDefinition data)
    {
        var modal = ModalService.Show<ReflexDeleteConfirmationModal>(new ModalOptions { Title = @Loc["ReflexListPage_DeleteDialogTitle"], ModalHeight = ModalSize.Small, ModalWidth = ModalSize.Medium });

        var result = await modal.Result;

        if (!result.Canceled)
        {
            ViewModel.DeleteReflexForTestOrder(data.Id);
        }
    }

    private async Task HoldTests()
    {
        await SetIsActive(false);
    }

    private async Task ReleaseTests()
    {
        await SetIsActive(true);
    }

    private async Task SetIsActive(bool isActive)
    {
        if (_selectedItems == null)
        {
            return;
        }

        var totalSelectedCount = _selectedItems.Count;
        var countThatCanSetIsActive = _selectedItems.Count(x => _statesThatCanBeHeldOrReleased.Contains(x.State));

        if (totalSelectedCount != countThatCanSetIsActive)
        {
            //Inform user that some selected tests cannot be held/released due to their state
            string message;
            string okText;
            string title;
            if (isActive)
            {
                message = Loc["TestListView_ReleaseTestsConfirmationMessage", countThatCanSetIsActive, totalSelectedCount];
                okText = Loc["TestListView_ReleaseTestsConfirmationButtonText", countThatCanSetIsActive];
                title = Loc["TestListView_ReleaseTestsConfirmationTitle"];
            }
            else
            {
                message = Loc["TestListView_HoldTestsConfirmationMessage", countThatCanSetIsActive, totalSelectedCount];
                okText = Loc["TestListView_HoldTestsConfirmationButtonText", countThatCanSetIsActive];
                title = Loc["TestListView_HoldTestsConfirmationTitle"];
            }

            var modal = ModalService.ShowConfirm(message,
                Loc["TestListView_HoldReleaseCancelButtonText"],
                okText,
                new ModalOptions{Status = ModalStatus.Info, Title = title, ModalHeight = ModalSize.XSmall, ModalWidth = ModalSize.XSmall}, true);
            var result = await modal.Result;
            if(result.Canceled)
            {
                return;
            }
        }

        var volumeCalculationAffected = false;

        foreach (var viewModel in _selectedItems.Where(x => _statesThatCanBeHeldOrReleased.Contains(x.State)))
        {
            viewModel.IsActive = isActive;
            volumeCalculationAffected = true;
        }

        if(volumeCalculationAffected)
        {
            await ViewModel.ReCalculateVolumeNeeded();
        }
    }
}