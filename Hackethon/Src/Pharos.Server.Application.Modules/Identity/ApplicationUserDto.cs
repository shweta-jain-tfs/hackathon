namespace Pharos.Server.Application.Identity;

public class ApplicationUserDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public string? Password { get; set; }
    public DateTimeOffset? LastLoginDateTimeOffset { get; set; }
    /// <summary>
    /// Computed expiration date derived on query from emitted timestamp + configured PasswordExpirationDays.
    /// Null when emitted timestamp is unknown or expiration is disabled.
    /// </summary>
    public DateTimeOffset? PasswordExpirationDateTimeOffset { get; set; }
    public bool IsActive { get; set; }
    public bool IsPinEnabled { get; set; }
    public RoleDto? Role { get; set; }
}
