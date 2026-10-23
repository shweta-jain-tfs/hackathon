using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class LabQualityControlTestListView
{
    private PhDataGrid<LabQualityControlTest>? _dataGrid;
    private HashSet<LabQualityControlTest> _selectedItems = [];

    [Parameter] public LabQualityControlDetailViewModel ViewModel { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = null!;

    private void SelectedItemsChanged(HashSet<LabQualityControlTest> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private async Task EditTest(LabQualityControlTest test)
    {
        var viewModel = new EditQualityControlTestViewModel(test.Id,
            test.ApplyAcceptanceRange,
            test.LowerLimit,
            test.UpperLimit,
            test.NumberOfTests,
            test.UseAcceptanceRange,
            test.Replicates,
            test.ReplicateCvLimit,
            test.UseLotQualification,
            test.InstrumentDilutionFactor,
            test.MaxCv,
            test.NumberOfSd,
            test.PreDilutionFactor,
            test.TestId,
            test.MethodName,
            test.TestName,
            test.QualityControlType,
            InstrumentDilutionService);

        var parameters = new ModalParameters { { nameof(EditQualityControlTestModalView.ViewModel), viewModel } };
        var model = ModalService.Show<EditQualityControlTestModalView>(new ModalOptions
        {
            Title = $"{Loc["EditQualityControlTestModalView_EditQcTest"]} - {test.MethodName} : {test.TestName}",
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.Large
        }, parameters);

        await viewModel.ConstructDilutionFactors();

        var result = await model.Result;

        if (!result.Canceled)
        {
            ViewModel.UpdateTest(viewModel);
        }
    }

    private async Task AddTests()
    {
        var model = ModalService.Show<AddLabQualityControlTestView>(new ModalOptions
        {
            Title = Loc["AddLabQualityControlTestView_AddQcTestTitle"],
            ModalHeight = ModalSize.XLarge,
            ModalWidth = ModalSize.Large
        }, new ModalParameters());
        var result = await model.Result;

        if (result is { Canceled: false, Data: EditQualityControlTestViewModel testToAdd })
        {
            ViewModel.AddTest(testToAdd);
        }
    }

    private void DeleteTests()
    {
        ViewModel.RemoveTests(_selectedItems);

        _selectedItems.Clear();
        _dataGrid?.ClearSelectedItem();
    }
}