using System.Text.Json.Serialization;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Instrument.Domain;

internal class AssaySchedulerDay : BaseEntity
{
    /// <summary>
    /// The ID of the associated scheduler.
    /// </summary>
    public int SchedulerId { get; set; }

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

    #region Navigation properties

    /// <summary>
    /// Navigation property for the associated scheduler.
    /// </summary>
    [JsonIgnore]
    public virtual AssayScheduler Scheduler { get; set; } = default!;

    /// <summary>
    /// Navigation property for the scheduled instrument methods.
    /// </summary>
    public virtual ICollection<AssaySchedulerMethod> ScheduledMethods { get; set; } = new List<AssaySchedulerMethod>();

    #endregion
}
