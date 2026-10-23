using System.ComponentModel;
using Microsoft.Extensions.Localization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;
using Pharos.Client.Common.Localization;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.ServiceTools.Views.RequestGenerator;

public partial class TestListView : IDisposable
{
    private PhDataGrid<RequestGeneratorTest>? _testGrid;

    [Parameter]
    public TestListViewModel TestsViewModel { get; set; } = null!;

    [Inject]
    public IStringLocalizer<Translations> Loc { get; set; } = default!;

    [Inject]
    public IModalService ModalService { get; set; } = default!;

    protected override void OnInitialized()
    {
        TestsViewModel.PropertyChanged += OnTestsViewModelPropertyChanged;
    }

    private async Task EditTestViewOpen(RequestGeneratorTest test)
    {
        var parameters = new ModalParameters
        {
            { nameof(EditTestView.Test), test }
        };

        var modal = ModalService.Show<EditTestView>(new ModalOptions { Title = Loc["TestEditView_Title"], ModalHeight = ModalSize.Large, ModalWidth = ModalSize.Small }, parameters);
        var result = await modal.Result;

        if (!result.Canceled && result.Data is RequestGeneratorTest updatedTest)
        {
            TestsViewModel.UpdateTest(test, updatedTest);
        }
    }

    private void DeleteSelectedTests()
    {
        TestsViewModel.DeleteSelectedTests();

        if (_testGrid is not null)
        {
            _testGrid.ClearSelectedItem();
        }
    }

    private void OnTestsViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        TestsViewModel.PropertyChanged -= OnTestsViewModelPropertyChanged;
        GC.SuppressFinalize(this);
    }
}
