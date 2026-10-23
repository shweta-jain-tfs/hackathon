namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class BarcodeFormatEditViewModel : ViewModelItem<QualityControlBarcodeFormatDetail>
{
    private readonly IBarcodeFormatBuilder _barcodeFormatBuilder;

    public BarcodeFormatEditViewModel(QualityControlBarcodeFormatDetail model, IBarcodeFormatBuilder barcodeFormatBuilder) : base(model)
    {
        _barcodeFormatBuilder = barcodeFormatBuilder;
        ConstructFormat();
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, v => Model.Name = v);
    }

    public int TotalLength
    {
        get => Model.TotalLength;
        set
        {
            SetProperty(Model.TotalLength, value, v => Model.TotalLength = v);
            ValidationNeeded();
            ConstructFormat();
        }
    }

    public int QcStartPosition
    {
        get => Model.QcStartPosition;
        set
        {
            SetProperty(Model.QcStartPosition, value, v => Model.QcStartPosition = v);
            ValidationNeeded();
            ConstructFormat();
        }
    }

    public int QcLength
    {
        get => Model.QcLength;
        set
        {
            SetProperty(Model.QcLength, value, v => Model.QcLength = v);
            ValidationNeeded();
            ConstructFormat();
        }
    }

    public bool ContainsLotInformation
    {
        get => Model.ContainsLotInformation;
        set
        {
            SetProperty(Model.ContainsLotInformation, value, v => Model.ContainsLotInformation = v);
            ValidationNeeded();
            ConstructFormat();
        }
    }

    public int LotStartPosition
    {
        get => Model.LotStartPosition;
        set
        {
            SetProperty(Model.LotStartPosition, value, v => Model.LotStartPosition = v);
            ValidationNeeded();
            ConstructFormat();
        }
    }

    public int LotLength
    {
        get => Model.LotLength;
        set
        {
            SetProperty(Model.LotLength, value, v => Model.LotLength = v);
            ValidationNeeded();
            ConstructFormat();
        }
    }

    private void ConstructFormat()
    {
        Format = _barcodeFormatBuilder.GetFormat(Model.TotalLength, Model.QcStartPosition, Model.QcLength, Model.ContainsLotInformation, Model.LotStartPosition, Model.LotLength);
    }

    public IReadOnlyList<FormatPosition> Format { get; set; } = [];

    public event EventHandler? ReValidate;

    private void ValidationNeeded()
    {
        ReValidate?.Invoke(this, EventArgs.Empty);
    }
}