using Pharos.Client.Common.Models;
using Pharos.Client.Common.ViewModels;
using DayOfWeek = Pharos.Client.Common.Models.DayOfWeek;

namespace Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;

public sealed class AssaySchedulerDayViewModel : ViewModelItem<AssaySchedulerDay>
{
    public AssaySchedulerDayViewModel(AssaySchedulerDay model, string instrumentName) : base(model)
    {
        InstrumentName = instrumentName;

        ScheduledMethods = model.ScheduledMethods?.Select(m => new AssaySchedulerMethodViewModel(m)).ToList() ?? [];
    }

    public int Id
    {
        get => Model.Id;
        set => SetProperty(Model.Id, value, v => Model.Id = v);
    }

    public DayOfWeek Day
    {
        get => Model.Day;
        set => SetProperty(Model.Day, value, v => Model.Day = v);
    }

    public TimeOnly? EndTime
    {
        get => Model.EndTime;
        set => SetProperty(Model.EndTime, value, v => Model.EndTime = v);
    }

    public string InstrumentName { get; }

    public TimeOnly? StartTime
    {
        get => Model.StartTime;
        set => SetProperty(Model.StartTime, value, v => Model.StartTime = v);
    }

    public List<AssaySchedulerMethodViewModel> ScheduledMethods { get; }

    public bool HasAnyValues =>
        StartTime != null ||
        EndTime != null ||
        (ScheduledMethods?.Any(m => m.IsActive) ?? false);

    public override bool IsModified => base.IsModified || ScheduledMethods.Any(x => x.IsModified);
}