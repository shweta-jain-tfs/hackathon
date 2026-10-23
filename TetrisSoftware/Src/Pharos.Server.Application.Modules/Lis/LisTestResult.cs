using Pharos.Server.Domain.Enums;

namespace Pharos.Server.Application.Lis;

/// <summary>
/// Represents the base class for test result.
/// </summary>
public record LisTestResult
{
    /// <summary>
    /// The allergen concentration converted to a string. If the value is null, the result is not ready
    /// or cannot be received due to an error.
    /// </summary>
    public string? Concentration { get; set; }

    /// <summary>
    /// The unit of measurement for the concentration (e.g., "kU/L", "mg/dL", etc.).
    /// </summary>
    public string? Units { get; set; }

    /// <summary>
    /// The response measurement time stamp.
    /// </summary>
    public DateTimeOffset? MeasurementDateTime { get; set; }

    /// <summary>
    /// Response value in RU.
    /// </summary>
    public string? Ru { get; init; }

    public List<LisReagent> Reagents { get; set; } = new();

    /// <summary>
    /// The name of instrument that performed the test
    /// </summary>
    public string? InstrumentName { get; set; }
}

public record LisReagent
{
    public ArticleType ReagentType { get; set; }

    public string? LotNumber { get; set; }
}
