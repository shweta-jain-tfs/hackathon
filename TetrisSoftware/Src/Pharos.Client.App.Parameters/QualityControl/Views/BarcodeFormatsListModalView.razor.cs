using Pharos.Client.App.Parameters.QualityControl.Models;
using Pharos.Client.App.Parameters.QualityControl.ViewModels;
using Pharos.Client.Common.App.Validation;
using Pharos.Common.Web.Components.DataGrid;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.QualityControl.Views;

public partial class BarcodeFormatsListModalView : IDisposable
{
    private enum ViewMode
    {
        List,
        Create,
        Edit
    }

    private PhDataGrid<QualityControlBarcodeFormatDetail>? _dataGrid;
    private HashSet<QualityControlBarcodeFormatDetail> _selectedItems = [];
    private ViewMode _currentView = ViewMode.List;
    private BarcodeFormatEditViewModel? _editViewModel;
    private PhSimpleValidationContext? _validationContext;
    private bool _isValid;
    private List<QualityControlBarcodeFormatDetail> _barcodeFormats = [];

    [CascadingParameter] public ModalInstance Modal { get; set; } = default!;
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IQualityControlBarcodeFormatItemSource DataSource { get; set; } = default!;
    [Inject] private IQualityControlBarcodeFormatNotificationProvider NotificationProvider { get; set; } = default!;
    [Inject] private IBarcodeFormatBuilder BarcodeFormatBuilder { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        _barcodeFormats = await DataSource.GetAll();
        NotificationProvider.RefreshNeeded += OnDataChanged;
    }

    private async void OnDataChanged(object? sender, EventArgs e)
    {
        _barcodeFormats = await DataSource.GetAll();
        StateHasChanged();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender && _currentView != ViewMode.List)
        {
            _validationContext?.Validate();
            _isValid = _validationContext?.IsValid() ?? false;
            StateHasChanged();
        }
    }

    private void SelectedItemsChanged(HashSet<QualityControlBarcodeFormatDetail> selectedItems)
    {
        _selectedItems = selectedItems;
    }

    private void CreateBarcodeFormat()
    {
        var model = new QualityControlBarcodeFormatDetail
        {
            Id = 0,
            Name = string.Empty,
            TotalLength = 1,
            QcStartPosition = 1,
            QcLength = 1,
            LotStartPosition = 1,
            LotLength = 1
        };

        _editViewModel = new BarcodeFormatEditViewModel(model, BarcodeFormatBuilder);
        _editViewModel.ReValidate += ValidationNeeded;
        _currentView = ViewMode.Create;

        Modal.UpdateTitle(Loc["BarcodeFormatsListModal_CreateTitle"]); 
    }

    private async Task EditBarcodeFormat(int id)
    {
        var model = await DataSource.GetDetails(id);
        _editViewModel = new BarcodeFormatEditViewModel(model, BarcodeFormatBuilder);
        _editViewModel.ReValidate += ValidationNeeded;
        _currentView = ViewMode.Edit;

        Modal.UpdateTitle(Loc["BarcodeFormatsListModal_EditTitle"]);
    }

    private void ValidationNeeded(object? sender, EventArgs e)
    {
        _validationContext?.Validate();
    }

    private async Task SaveBarcodeFormat()
    {
        if (_editViewModel == null)
        {
            return;
        }

        if (_currentView == ViewMode.Create)
        {
            await DataSource.CreateAsync(_editViewModel.Model);
        }
        else if (_currentView == ViewMode.Edit)
        {
            await DataSource.UpdateAsync(_editViewModel.Model);
        }

        _barcodeFormats = await DataSource.GetAll();
        BackToList();
    }

    private void CancelEdit()
    {
        BackToList();
    }

    private void BackToList()
    {
        _currentView = ViewMode.List;
        if (_editViewModel != null)
        {
            _editViewModel.ReValidate -= ValidationNeeded;
        }

        _editViewModel = null;
        Modal.UpdateTitle(Loc["BarcodeFormatsListModal_ListTitle"]);

        StateHasChanged();
    }

    private async Task DeleteBarcodeFormats()
    {
        var ids = _selectedItems.Select(x => x.Id).ToHashSet();
        await DataSource.DeleteAsync(ids);

        _selectedItems.Clear();
        _dataGrid?.ClearSelectedItem();
        StateHasChanged();
    }

    private void ValidationChanged(ValidationContextState obj)
    {
        _isValid = obj.IsValid;
    }

    private string ConstructFormat(QualityControlBarcodeFormatDetail item)
    {
        return BarcodeFormatBuilder.GetString(item.TotalLength, item.QcStartPosition, item.QcLength, item.ContainsLotInformation, item.LotStartPosition, item.LotLength);
    }

    public void Dispose()
    {
        NotificationProvider.RefreshNeeded -= OnDataChanged;
        if (_editViewModel != null)
        {
            _editViewModel.ReValidate -= ValidationNeeded;
            _editViewModel.Dispose();
        }

        _dataGrid?.Dispose();
        _validationContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}