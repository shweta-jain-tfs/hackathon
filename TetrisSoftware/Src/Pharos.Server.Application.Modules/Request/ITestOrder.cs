using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Request;


/// <summary>
/// The state in which a database entity is.
/// </summary>
public enum EntityState
{
    Unchanged,
    Modified,
    Added
}

/// <summary>
/// Represents a test order.
/// </summary>
public interface ITestOrder
{
    /// <summary>
    /// The globally unique test order ID which is shared with the instrument.
    /// </summary>
    public Guid Uid { get; }

    /// <summary>The processing status of the order.</summary>
    public TestOrderState State { get; }

    /// <summary>
    /// The tube id that this test order is assigned to.
    /// </summary>
    public string? TubeId { get; }

    /// <summary>
    /// Indicates whether the test order has reflex tests.
    /// </summary>
    public bool HasReflexTests { get; }

    /// <summary>
    /// Indicates whether the test order is active or not.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// The LIS channel id from which this test order was imported.
    /// </summary>
    public int? LisChannelId { get; }

    /// <summary>
    /// The state in which the test order entity is after request has been updated.
    /// </summary>
    EntityState EntityState { get; }
}
