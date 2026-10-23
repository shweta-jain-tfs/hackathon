using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Reflex;

namespace Pharos.Server.Request.Domain;

/// <summary>
/// Represents a reflex definition (along with condition and tests) added to a specific test.
/// </summary>
internal class ReflexDefinition : BaseEntity, IReflexDefinition
{
    /// <summary>
    /// The ID of the test order to which this reflex definition is attached to .
    /// </summary>
    public Guid TestOrderId { get; set; }

    /// <inheritdoc cref="IReflexDefinition.Name" />
    [MaxLength(Constants.ReflexNameLength)]
    public string Name { get; set; } = default!;

    /// <inheritdoc cref="IReflexDefinition.Automatic" />
    public bool Automatic { get; set; }

    /// <inheritdoc cref="IReflexDefinition.RequestorId" />
    [MaxLength(Constants.RequestorIdLength)]
    public string? RequestorId { get; set; }

    /// <inheritdoc cref="IReflexDefinition.ReflexCondition" />
    public ReflexCondition ReflexCondition { get; set; } = new();

    /// <inheritdoc cref="IReflexDefinition.ReflexTests" />
    public IList<ReflexTest> ReflexTests { get; set; } = new List<ReflexTest>();
}