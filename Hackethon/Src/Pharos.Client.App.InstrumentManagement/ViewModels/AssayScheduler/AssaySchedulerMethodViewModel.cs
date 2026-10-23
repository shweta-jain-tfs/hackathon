using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;

public class AssaySchedulerMethodViewModel(AssaySchedulerMethod model) : ViewModelItem<AssaySchedulerMethod>(model)
{
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

    public InstrumentMethodWhenToRun WhenToRun
    {
        get => (InstrumentMethodWhenToRun)Model.WhenToRun;
        set => SetProperty((InstrumentMethodWhenToRun)Model.WhenToRun, value, v => Model.WhenToRun = (int)v);
    }
}
