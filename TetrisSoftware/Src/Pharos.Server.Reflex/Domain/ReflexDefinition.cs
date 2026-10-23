using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Reflex.Domain;

/// <inheritdoc cref="IReflexDefinition" />
internal class ReflexDefinition : BaseAuditableEntity, IReflexDefinition
{
    /// <inheritdoc cref="IReflexDefinition.Name" />
    [MaxLength(Constants.ReflexNameLength)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// The value indicating whether the reflex definition is used in test processing.
    /// </summary>
    public bool Active { get; set; }

    /// <inheritdoc cref="IReflexDefinition.Automatic" />
    public bool Automatic { get; set; }

    /// <summary>
    /// The external ID of the person (e.g. doctor in a hospital) who requested these reflex tests.
    /// If this property is set, the reflex definition will only match test orders from this requestor.
    /// </summary>
    [MaxLength(Constants.RequestorIdLength)]
    public string? RequestorId { get; set; }

    /// <inheritdoc cref="IReflexDefinition.ReflexCondition" />
    public ReflexCondition ReflexCondition { get; set; } = new();

    /// <inheritdoc cref="IReflexDefinition.ReflexTests" />
    public IList<ReflexTest> ReflexTests { get; set; } = new List<ReflexTest>();
}