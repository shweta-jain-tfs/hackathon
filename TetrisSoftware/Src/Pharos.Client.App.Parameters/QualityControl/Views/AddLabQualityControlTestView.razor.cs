using Pharos.Client.App.Parameters.Common.ViewModels;
using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class AddLabQualityControlTestView : IDisposable
{
    private EditQualityControlTestViewModel? _qualityControlTestViewModel;

    private readonly List<MethodTest> _predefinedItems = [];
    private bool _chooseTest = true;

    private Dictionary<string, string> _methodList = new();

    private MethodTest? _selectedItem;
    private bool _isValid = true;
    private PhSimpleValidationContext? _validationContext;

    [Inject] private MethodListViewModel ViewModel { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IInstrumentDilutionService InstrumentDilutionService { get; set; } = null!;

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        ViewModel.ViewRefreshNeeded += OnViewRefreshNeeded;
        _methodList = await ViewModel.GetMultiFilterDataSource();

        _qualityControlTestViewModel = new EditQualityControlTestViewModel(
            id: 0,
            applyAcceptanceRange: false,
            lowerLimit: EditQualityControlTestViewModel.DefaultLowerLimit,
            upperLimit: EditQualityControlTestViewModel.DefaultUpperLimit,
            numberOfTests: EditQualityControlTestViewModel.DefaultNumberOfTests,
            useAcceptanceRange: false,
            replicates: 1,
            replicateCvLimit: 0,
            useLotQualification: false,
            instrumentDilutionFactor: 1,
            maxCv: EditQualityControlTestViewModel.DefaultMaxCv,
            numberOfSd: 1,
            preDilutionFactor: 1,
            testId: 0,
            methodName: string.Empty,
            testName: string.Empty,
            qualityControlType: QualityControlType.Concentration,
            instrumentDilutionService: InstrumentDilutionService);

        _qualityControlTestViewModel.ReValidate += ValidationNeeded;
    }

    private void ValidationNeeded(object? sender, EventArgs e)
    {
        _validationContext?.Validate();
    }

    private async Task CloseModal(ModalResult result)
    {
        await Modal.CloseAsync(result);
    }

    private async Task SelectedTestChanged(MethodTest? selectedTest)
    {
        if (_qualityControlTestViewModel is null)
        {
            return;
        }

        _selectedItem = selectedTest;
        _predefinedItems.Clear();
        if (selectedTest == null)
        {
            _qualityControlTestViewModel.RemoveTest();
            return;
        }

        _predefinedItems.Add(selectedTest);
        await _qualityControlTestViewModel.SetTest(selectedTest);
    }

    private void OnViewRefreshNeeded(object? sender, EventArgs e)
    {
        StateHasChanged();
    }

    private void EditDetails()
    {
        _chooseTest = false;
    }

    private void ChooseTest()
    {
        _chooseTest = true;
    }

    private async Task Apply()
    {
        await Modal.CloseAsync(ModalResult.Ok(_qualityControlTestViewModel));
    }

    private static bool PredefinedComparer(MethodTest methodTest, object obj)
    {
        var test = (MethodTest)obj;
        return methodTest.Id == test.Id;
    }

    private void ValidationChanged(ValidationContextState obj)
    {
        _isValid = obj.IsValid;
    }

    public void Dispose()
    {
        ViewModel.ViewRefreshNeeded -= OnViewRefreshNeeded;
        _qualityControlTestViewModel?.ReValidate -= ValidationNeeded;
        _validationContext?.Dispose();
        ViewModel.Dispose();

        GC.SuppressFinalize(this);
    }
}