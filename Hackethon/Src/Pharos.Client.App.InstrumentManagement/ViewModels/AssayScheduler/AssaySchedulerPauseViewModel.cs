namespace Pharos.Client.App.InstrumentManagement.ViewModels.AssayScheduler;

public enum PauseType
{
    Manual,
    Timed
}

public class AssaySchedulerPauseViewModel
{
    public DateTimeOffset? PausedUntilDate { get; set; }

    public PauseType PauseType
    {
        get;
        set
        {
            field = value;
            PausedUntilDate = field switch
            {
                PauseType.Manual => null,
                PauseType.Timed when PausedUntilDate == null => DateTimeOffset.Now,
                _ => PausedUntilDate
            };
        }
    } = PauseType.Manual;
}