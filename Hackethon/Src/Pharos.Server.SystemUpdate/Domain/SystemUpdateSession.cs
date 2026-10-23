using System.ComponentModel.DataAnnotations;

namespace Pharos.Server.SystemUpdate.Domain;

internal enum UpdateSource
{
    Usb,
    Remote
}

internal enum UpdateStatus
{
    Pending,
    Successful,
    Failed
}

internal sealed class SystemUpdateSession
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public UpdateSource Source { get; init; }

    [MaxLength(2)]
    public string UsbDrive { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PreviousVersion { get; init; } = "0.0.0.0";

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public UpdateStatus Status { get; set; } = UpdateStatus.Pending;

    public DateTimeOffset? AcknowledgedAtUtc { get; set; }
}
