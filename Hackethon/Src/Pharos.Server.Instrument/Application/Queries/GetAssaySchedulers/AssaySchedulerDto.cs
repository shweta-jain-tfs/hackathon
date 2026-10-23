using Pharos.Contracts;

namespace Pharos.Server.Instrument.Application.Queries.GetAssaySchedulers;

public class AssaySchedulerDto
{
    public int Id { get; set; }

    /// <summary>
    /// The name of the instrument associated with this scheduler.
    /// </summary>
    public string? InstrumentName { get; set; }

    /// <summary>
    /// Indicates whether the scheduler is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The date until which the scheduler is paused. Null if not paused.
    /// </summary>
    public DateTimeOffset? PausedUntilDate { get; set; }

    /// <summary>
    /// Days of the week for one instrument scheduler.
    /// </summary>
    public List<AssaySchedulerDayDto>? AssaySchedulerDays { get; set; }
}