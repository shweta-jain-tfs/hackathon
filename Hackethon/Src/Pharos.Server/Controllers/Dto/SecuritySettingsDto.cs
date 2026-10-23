namespace Pharos.Server.Controllers.Dto;

public class SecuritySettingsDto
{
    public required bool PasswordRequireDigit { get; set; }
    public required bool PasswordRequireNonAlphanumeric { get; set; }
    public required bool PasswordRequiresBothUpperAndLowerCase { get; set; }
    public required int PasswordRequiredLength { get; set; }
    public required bool PasswordRequireNoUsername { get; set; }
    public required bool PasswordExpirationEnabled { get; set; }
    public required bool EnablePinLogin { get; set; }
    public required bool AutoLogoutEnabled { get; set; }
    public required int AutoLogoutTimeoutMinutes { get; set; }
}
