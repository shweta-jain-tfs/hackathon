using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Instrument.Domain;

internal class AssaySchedulerMethod : BaseEntity
{
    /// <summary>
    /// The ID of the associated scheduler day.
    /// </summary>
    public int SchedulerDayId { get; set; }

    /// <summary>
    /// The name of the instrument method.
    /// </summary>
    [MaxLength(Constants.MethodNameLength)]
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the method is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates when to run calibrators.
    /// </summary>
    public InstrumentMethodWhenToRun WhenToRun { get; set; }

    #region Navigation properties

    /// <summary>
    /// Navigation property for the associated scheduler day.
    /// </summary>
    [JsonIgnore]
    public virtual AssaySchedulerDay SchedulerDay { get; set; } = default!;

    #endregion
}
