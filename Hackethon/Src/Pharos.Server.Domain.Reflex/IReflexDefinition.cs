namespace Pharos.Server.Domain.Reflex;

/// <summary>
/// Represents the definition of a reflex, containing condition and tests.
/// </summary>
public interface IReflexDefinition
{
    int Id { get; set; }

    /// <summary>
    /// The name of the reflex definition.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// The value indicating whether the reflex tests are added to a request automatically
    /// (when the reflex condition is fulfilled) or they require user approval.
    /// </summary>
    bool Automatic { get; set; }

    /// <summary>
    /// The external ID of the person (e.g. doctor in a hospital) who requested these reflex tests.
    /// If this property is set, the reflex definition will only match test orders from this requestor.
    /// </summary>
    public string? RequestorId { get; set; }

    /// <summary>
    /// The condition that must be evaluated in order to add reflex tests to a request.
    /// </summary>
    ReflexCondition ReflexCondition { get; set; }

    /// <summary>
    /// The collection of reflex tests that are associated with this reflex definition.
    /// </summary>
    IList<ReflexTest> ReflexTests { get; }
}
