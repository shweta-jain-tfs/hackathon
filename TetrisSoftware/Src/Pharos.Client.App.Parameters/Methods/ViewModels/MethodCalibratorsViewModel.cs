namespace Pharos.Client.App.Parameters.Methods.ViewModels;

public sealed class MethodCalibratorsViewModel : ViewModelItem<MethodCalibratorCurve>
{
    public MethodCalibratorsViewModel(MethodCalibratorCurve model) : base(model)
    {
    }

    public bool AllowMonthlyCalibration { get; set; }

    public int MaxNumberOfDays
    {
        get => Model.MaxNumberOfDays;
        set => SetProperty(Model.MaxNumberOfDays, value, v => Model.MaxNumberOfDays = v);
    }

    public int DisplayedMaxNumberOfDays
    {
        get => AllowMonthlyCalibration ? MaxNumberOfDays : 0;
        set
        {
            if (AllowMonthlyCalibration)
            {
                MaxNumberOfDays = value;
            }
            else
            {
                MaxNumberOfDays = 0;
            }
        }
    }

    public int DefaultMaxNumberOfDays => Model.DefaultMaxNumberOfDays;
}