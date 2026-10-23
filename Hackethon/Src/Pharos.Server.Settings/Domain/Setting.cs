using System.ComponentModel.DataAnnotations;
using Pharos.Server.Domain.Common;

namespace Pharos.Server.Settings.Domain;

internal class Setting : IAuditModificationsEntity
{
    public const int KeyMaxLength = 1024;
    public const int ValueMaxLength = 2048;

    [MaxLength(KeyMaxLength)]
    public string Key { get; set; } = default!;

    [MaxLength(ValueMaxLength)]
    public string? Value { get; set; }

    [ProtectedData]
    [MaxLength(ValueMaxLength)]
    public string? ProtectedValue { get; set; }

    public DateTimeOffset? LastModified { get; set; }

    public Guid? LastModifiedBy { get; set; }
}
