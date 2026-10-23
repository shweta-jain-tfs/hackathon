using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.LabCommunity.Domain;

internal sealed class Enrollment
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    [MaxLength(Constants.LabCommunityLaboratoryCodeLength)]
    public string LaboratoryCode { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string DeviceId { get; init; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [ProtectedData]
    public string ApiKey { get; init; } = string.Empty;

    public DateTimeOffset EnrolledAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public bool ValidCredentials { get; set; } = true;
}
