using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Domain;

namespace Pharos.Server.LabCommunity.Domain;

[Index(nameof(DeduplicationKey), IsUnique = true)]
internal sealed class ExportItem
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    public ExportItemType ExportItemType { get; init; }

    [Required]
    [MaxLength(256)]
    public string DeduplicationKey { get; init; } = string.Empty;

    [Required]
    public string PayloadJson { get; init; } = string.Empty;

    public int PayloadVersion { get; init; }

    public ExportItemStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    [MaxLength(Constants.ExportItemLastErrorLength)]
    public string? LastError { get; set; }
}

internal enum ExportItemType
{
    InstrumentError = 1,
    CurveControlResult = 2,
    QualityControlResult = 3,
    BlankResult = 4
}

internal enum ExportItemStatus
{
    Pending = 0,
    Failed = 1
}