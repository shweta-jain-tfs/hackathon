using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.Dashboard;

public class AssaySelectRunViewModel(InstrumentMethod model) : ViewModelItem<InstrumentMethod>(model), IEquatable<AssaySelectRunViewModel>
{
    public int Id { get; set; }

    public int CalibrationValidForDays
    {
        get => Model.CalibrationValidForDays;
        set => SetProperty(Model.CalibrationValidForDays, value, v => Model.CalibrationValidForDays = v);
    }

    public bool IsActive
    {
        get => Model.IsActive;
        set => SetProperty(Model.IsActive, value, v => Model.IsActive = v);
    }

    public string MethodName
    {
        get => Model.MethodName;
        set => SetProperty(Model.MethodName, value, v => Model.MethodName = v);
    }

    public bool Calibrator
    {
        get => Model.Calibrator;
        set => SetProperty(Model.Calibrator, value, v => Model.Calibrator = v);
    }

    public bool ForceCalibrator
    {
        get => Model.ForceCalibrator;
        set => SetProperty(Model.ForceCalibrator, value, v => Model.ForceCalibrator = v);
    }

    public bool CalibratorOptionDisabled { get; set; }

    public InstrumentMethodWhenToRun WhenToRun
    {
        get => Model.WhenToRun;
        set => SetProperty(Model.WhenToRun, value, v => Model.WhenToRun = v);
    }

    public bool Equals(AssaySelectRunViewModel? other)
    {
        return Model.Equals(other?.Model);
    }

    public override bool Equals(object? obj)
    {
        return obj is AssaySelectRunViewModel other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Model.GetHashCode();
    }
}