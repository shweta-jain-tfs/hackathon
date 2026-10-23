using System.ComponentModel;
using Pharos.Client.App.Parameters.Panels.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Client.Common.ViewModels.Interfaces;
using Pharos.Contracts.Security;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Panels.Views;

public partial class PanelDetailsPage : DetailsPageBase<PanelsDetailsViewModel, MethodPanelDetails>
{
    private PhValidationContext? _validation;
    private PhDataGrid<PanelTestViewModel>? _dataGrid;
    private HashSet<PanelTestViewModel> _selectedTests = [];

    [Inject] private IAuthorizationService AuthorizationService { get; set; } = default!;

    private bool IsEditingDisabled { get; set; } = true;

    public PanelDetailsPage(
        PanelsDetailsPageViewModel pageViewModel,
        IStringLocalizer<Translations> loc,
        NavigationManager navigation,
        IModalService modal,
        IToasterService toasterService,
        IExceptionHandlerService exceptionHandler) : base(pageViewModel, loc, navigation, modal, toasterService, exceptionHandler)
    {
    }

    private string Title => DetailViewModel?.IsNewEntity == true ? Loc["PanelDetailsPage_NewPanel"] : DetailViewModel?.Model.PanelName ?? string.Empty;

    private string TestsHeader => Loc["PanelDetailsPage_TestsHeader", DetailViewModel?.Tests.Count ?? 0];

    private bool HasSelectedTests => _selectedTests.Count > 0;

    protected override string ParentPage => PageRoute.Parameters.TestPanels;

    protected override string DetailsPage(int id)
    {
        return PageRoute.Parameters.TestPanelDetails.DetailsPage(id);
    }

    protected override async Task OnInitializedAsync()
    {
        IsEditingDisabled = !await AuthorizationService.IsAuthorized(Permissions.EditSystemSettings);
    }

    protected override List<BreadcrumbItem> GetInitialBreadcrumbs()
    {
        return
        [
            new BreadcrumbItem(Loc["NavMenu_Parameters"], PageRoute.Parameters.Default),
            new BreadcrumbItem(Loc["TestPanelPage_Header"], ParentPage),
            new BreadcrumbItem("...", "")
        ];
    }

    protected override void AddBreadcrumbForViewModel()
    {
        Breadcrumbs[^1] = new BreadcrumbItem(Title, "");
    }

    protected override void SubscribeToViewModel()
    {
        DetailViewModel!.PropertyChanged += DetailViewModelOnPropertyChanged;
    }

    private void DetailViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PanelsDetailsViewModel.IsModified) or nameof(PanelsDetailsViewModel.PanelName))
        {
            StateHasChanged();
        }
    }

    protected override async Task<bool> SaveChanges(bool invokedFromNavigationLock)
    {
        if (DetailViewModel is null || IsEditingDisabled)
        {
            return false;
        }

        _validation?.Validate();

        if (!IsValid())
        {
            return false;
        }

        DetailViewModel.NormalizeSortOrder();
        var saved = await SaveChanges(DetailViewModel.Model, DetailViewModel.Model.Id, invokedFromNavigationLock);
        if (saved)
        {
            NavigateToParentList();
        }

        return saved;
    }

    private bool IsSaveEnabled()
    {
        return DetailViewModel is { IsModified: true }
            && !IsEditingDisabled
            && !string.IsNullOrWhiteSpace(DetailViewModel.PanelName)
            && IsValid();
    }

    private bool IsValid()
    {
        return _validation?.IsValid() ?? true;
    }

    protected override void Disposing()
    {
        if (DetailViewModel is not null)
        {
            DetailViewModel.PropertyChanged -= DetailViewModelOnPropertyChanged;
        }
    }

    protected override void OnDiscardChanges()
    {
        _selectedTests.Clear();
        _dataGrid?.ClearSelectedItem();
    }

    private void SelectedTestsChanged(HashSet<PanelTestViewModel> selectedTests)
    {
        _selectedTests = selectedTests;
    }

    private void AddIndexToTestSortOrder(int addIndexToTestSortOrder)
    {
        if (_selectedTests.Count != 1 || DetailViewModel is null)
        {
            return;
        }

        var selectedRow = _selectedTests.First();
        var oldSortOrder = selectedRow.SortOrder ?? 0;
        var newSortOrder = oldSortOrder + addIndexToTestSortOrder;

        var rowToSwapWith = DetailViewModel.Tests.FirstOrDefault(x => x.SortOrder == newSortOrder);
        if (rowToSwapWith is not null)
        {
            rowToSwapWith.SortOrder = oldSortOrder;
        }

        selectedRow.SortOrder = newSortOrder;
        StateHasChanged();
    }

    private bool MoveTestOrderDownDisabled()
    {
        if (_selectedTests.Count != 1 || DetailViewModel is null || DetailViewModel.Tests.Count == 0)
        {
            return true;
        }

        var selectedRow = _selectedTests.First();
        return selectedRow.SortOrder == DetailViewModel.Tests.Max(x => x.SortOrder);
    }

    private bool MoveTestOrderUpDisabled()
    {
        if (_selectedTests.Count != 1 || DetailViewModel is null || DetailViewModel.Tests.Count == 0)
        {
            return true;
        }

        var selectedRow = _selectedTests.First();
        return selectedRow.SortOrder == DetailViewModel.Tests.Min(x => x.SortOrder);
    }

    private async Task TestsAddViewOpen()
    {
        if (DetailViewModel is null)
        {
            return;
        }

        var parameters = new ModalParameters
        {
            { nameof(PanelTestAddView.CurrentPanelTests), DetailViewModel.Tests }
        };

        var modal = Modal.Show<PanelTestAddView>(new ModalOptions { Title = Loc["TestAddView_Title"], ModalHeight = ModalSize.XLarge, ModalWidth = ModalSize.Large }, parameters);
        var result = await modal.Result;

        if (!result.Canceled)
        {
            var data = (HashSet<MethodTest>?)result.Data;
            DetailViewModel.AddMethodTests(data);
        }
    }

    private async Task DeleteSelectedTests()
    {
        if (DetailViewModel is null)
        {
            return;
        }

        DetailViewModel.DeleteTests(_selectedTests);
        _selectedTests.Clear();
        if (_dataGrid is not null)
        {
            await _dataGrid.ClearSelectedItem();
        }

        SelectedTestsChanged(_selectedTests);
        StateHasChanged();
        await Task.Yield();
        StateHasChanged();
    }

    private async Task TestEditViewOpen(PanelTestViewModel testViewModel)
    {
        var test = new MethodPanelTest
        {
            Id = testViewModel.Model.Id,
            TestName = testViewModel.TestName,
            MethodName = testViewModel.MethodName,
            FullMethodName = testViewModel.FullMethodName,
            FullTestName = testViewModel.FullTestName,
            Replicates = testViewModel.Replicates,
            InstrumentDilutionFactor = testViewModel.InstrumentDilutionFactor,
            SortOrder = testViewModel.SortOrder
        };

        var viewModel = new PanelTestViewModel(test);
        try
        {
            var parameters = new ModalParameters
            {
                { nameof(PanelTestEditView.ViewModel), viewModel },
                { nameof(PanelTestEditView.IsEditingDisabled), IsEditingDisabled }
            };

            var modal = Modal.Show<PanelTestEditView>(new ModalOptions { Title = Loc["TestEditView_Title"], ModalHeight = ModalSize.Large, ModalWidth = ModalSize.Medium }, parameters);
            var result = await modal.Result;

            if (result is { Canceled: false, Data: PanelTestViewModel editedTest })
            {
                testViewModel.Replicates = editedTest.Replicates;
                testViewModel.InstrumentDilutionFactor = editedTest.InstrumentDilutionFactor;
            }
        }
        finally
        {
            viewModel.Dispose();
        }
    }
}
