namespace Pharos.Contracts;

public class AssaySchedulerDayDto
{
    public int Id { get; set; }

    /// <summary>
    /// The day of the week.
    /// </summary>
    public DayOfWeek Day { get; set; }

    /// <summary>
    /// The start time for the scheduler on this day. Null if no specific start time is set.
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// The end time for the scheduler on this day. Null if no specific end time is set.
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Scheduled instrument methods for this day.
    /// </summary>
    public List<AssaySchedulerMethodDto>? ScheduledMethods { get; set; }
}
