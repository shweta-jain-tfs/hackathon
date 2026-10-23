using System.Text.Json.Serialization;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Instrument.Domain;

internal class AssayScheduler : BaseEntity
{
    public int InstrumentId { get; set; }

    /// <summary>
    /// Indicates whether the scheduler is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The date until which the scheduler is paused. Null if not paused.
    /// </summary>
    public DateTimeOffset? PausedUntilDate { get; set; }

    #region Navigation properties

    /// <summary>
    /// Navigation property for the associated instrument.
    /// </summary>
    [JsonIgnore]
    public virtual Instrument Instrument { get; set; } = default!;

    /// <summary>
    /// Navigation property for the scheduled days.
    /// </summary>
    public virtual ICollection<AssaySchedulerDay> SchedulerDays { get; set; } = new List<AssaySchedulerDay>();

    #endregion
}