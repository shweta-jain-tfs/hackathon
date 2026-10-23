using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters;

namespace Pharos.Server.Application.Quality;

/// <summary>
/// Information extracted from a QC cartridge data matrix barcode.
/// This is a standalone DTO, not related to reagent lot information.
/// </summary>
public record QcCartridgeInformationDto
{
    /// <summary>
    /// The article number (3 characters from data matrix).
    /// </summary>
    public string ArticleId { get; init; } = string.Empty;

    /// <summary>
    /// The full lot number combining article number and lot suffix.
    /// </summary>
    public string LotNumber { get; init; } = string.Empty;

    /// <summary>
    /// The display lot number (2 characters from data matrix).
    /// </summary>
    public string DisplayLotNumber { get; init; } = string.Empty;

    /// <summary>
    /// The version of the QC data matrix encoding.
    /// </summary>
    public string Version { get; init; } = "1";

    /// <summary>
    /// The type of the article.
    /// </summary>
    public ArticleType ArticleType { get; init; }

    /// <summary>
    /// The friendly display name of the article.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// The name of the method this article is associated with.
    /// </summary>
    public string? MethodName { get; init; }

    /// <summary>
    /// The ID of the method this article is associated with.
    /// </summary>
    public int? MethodId { get; init; }

    /// <summary>
    /// The shape of the article indicator in the UI.
    /// </summary>
    public ArticleIndicatorShape Shape { get; set; }

    /// <summary>
    /// The colors of the article indicator in the UI.
    /// </summary>
    public List<string?> Colors { get; set; } = default!;

    /// <summary>
    /// The text labels for the article indicator in the UI.
    /// </summary>
    public List<string?> Texts { get; init; } = default!;

    /// <summary>
    /// The list of tests and their limits encoded in the QC data matrix.
    /// </summary>
    public IReadOnlyList<QcCartridgeTestInfo> Tests { get; init; } = [];

    /// <summary>
    /// Indicates whether the QC ranges from this cartridge were newly registered in the database.
    /// True if ranges were saved for the first time; false if they already existed.
    /// </summary>
    public bool IsNewlyRegistered { get; init; }
}

/// <summary>
/// Represents a QC test with its name, limits, and concentration unit.
/// </summary>
public record QcCartridgeTestInfo
{
    /// <summary>
    /// The test name/identifier (2-5 alphanumeric characters).
    /// </summary>
    public string TestName { get; init; } = string.Empty;

    /// <summary>
    /// The list of limits associated with this test.
    /// </summary>
    public IReadOnlyList<QcCartridgeTestLimit> Limits { get; init; } = [];

    /// <summary>
    /// The concentration unit for this test (e.g., "kUA/l").
    /// Empty if the unit could not be determined from method parameters.
    /// </summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// The Id for this test and qc cartridge combination, used for linking to the QC test details page.
    /// </summary>
    public int? QcTestId { get; init; }

    /// <summary>
    /// Indicates whether the QC test can be opened from the QC test details page.
    /// </summary>
    public bool QcTestEnabled { get; init; }
}

/// <summary>
/// Represents a single QC test limit.
/// </summary>
public record QcCartridgeTestLimit
{
    /// <summary>
    /// The comparison type for the limit.
    /// </summary>
    public QcCartridgeLimitType LimitType { get; init; }

    /// <summary>
    /// The numeric limit value.
    /// </summary>
    public double LimitValue { get; init; }
}

/// <summary>
/// Defines the comparison operators for QC test limits.
/// </summary>
public enum QcCartridgeLimitType
{
    /// <summary>Less than (&lt;)</summary>
    LessThan,

    /// <summary>Less than or equal (?)</summary>
    LessThanOrEqual,

    /// <summary>Greater than (&gt;)</summary>
    GreaterThan,

    /// <summary>Greater than or equal (?)</summary>
    GreaterThanOrEqual,

    /// <summary>Equal (=)</summary>
    Equal
}
