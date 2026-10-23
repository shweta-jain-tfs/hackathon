using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Security;

[RootConfigSection(sortOrder: 90)]
[DisplayNameLocal("SecuritySettings")]
public class SecuritySettings : IConfigSectionWithInitialValues
{
    /// <summary>
    /// Controls whether and when inactive users are automatically logged out.
    /// </summary>
    [SettingsGroup]
    [DisplayNameLocal("SecuritySettings_AutoLogoutSettings")]
    public AutoLogoutSettings AutoLogoutSettings { get; set; } = new();

    /// <summary>
    /// The settings for Password complexity.
    /// </summary>
    [SettingsGroup]
    [DisplayNameLocal("SecuritySettings_PasswordComplexitySettings")]
    public PasswordComplexitySettings PasswordComplexitySettings { get; set; } = new();

    /// <summary>
    /// Indicates whether password expiration is enabled.
    /// </summary>
    [DisplayNameLocal("SecuritySettings_PasswordExpirationEnabled")]
    public bool PasswordExpirationEnabled { get; set; } = true;

    /// <summary>
    /// Password lifetime in days before expiration is enforced.
    /// Valid range: 1-365 days. Default: 90 days.
    /// </summary>
    [DependsFrom(nameof(PasswordExpirationEnabled), "True", DependencyType.Hide)]
    [DisplayNameLocal("SecuritySettings_PasswordExpirationDays")]
    [SettingsRange(1, 365, "days")]
    public int PasswordExpirationDays { get; set; } = 90;

    /// <summary>
    /// Indicates whether username/password lockout is enabled after consecutive failed attempts.
    /// </summary>
    [DisplayNameLocal("SecuritySettings_EnablePasswordLoginLockout")]
    public bool EnablePasswordLoginLockout { get; set; } = true;

    /// <summary>
    /// The number of consecutive failed username/password attempts that trigger lockout.
    /// Valid range: 1-10 attempts. Default: 5 attempts.
    /// </summary>
    [DependsFrom(nameof(EnablePasswordLoginLockout), "True", DependencyType.Hide)]
    [DisplayNameLocal("SecuritySettings_PasswordLoginLockoutMaxFailedAttempts")]
    [SettingsRange(1, 10, "attempts")]
    public int PasswordLoginLockoutMaxFailedAttempts { get; set; } = 3;

    /// <summary>
    /// The duration (in minutes) for which username/password login is disabled after consecutive failed attempts.
    /// Valid range: 1-240 minutes. Default: 5 minutes.
    /// </summary>
    [DependsFrom(nameof(EnablePasswordLoginLockout), "True", DependencyType.Hide)]
    [DisplayNameLocal("SecuritySettings_PasswordLoginLockoutDurationMinutes")]
    [SettingsRange(5, 240, "minutes")]
    public int PasswordLoginLockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Indicates whether PIN login is enabled for users.
    /// </summary>
    [DisplayNameLocal("SecuritySettings_EnablePinLogin")]
    public bool EnablePinLogin { get; set; } = false;

    /// <summary>
    /// Indicates whether PIN login lockout is enabled after consecutive failed attempts.
    /// </summary>
    [DisplayNameLocal("SecuritySettings_EnablePinLoginLockout")]
    public bool EnablePinLoginLockout { get; set; } = true;

    /// <summary>
    /// The number of consecutive failed PIN attempts that trigger lockout.
    /// Valid range: 1-10 attempts. Default: 3 attempts.
    /// </summary>
    [DependsFrom(nameof(EnablePinLoginLockout), "True", DependencyType.Hide)]
    [DisplayNameLocal("SecuritySettings_PinLoginLockoutMaxFailedAttempts")]
    [SettingsRange(1, 10, "attempts")]
    public int PinLoginLockoutMaxFailedAttempts { get; set; } = 3;

    /// <summary>
    /// The duration (in minutes) for which PIN login is disabled after consecutive failed attempts.
    /// Valid range: 5-240 minutes. Default: 15 minutes.
    /// </summary>
    [DependsFrom(nameof(EnablePinLoginLockout), "True", DependencyType.Hide)]
    [DisplayNameLocal("SecuritySettings_PinLoginLockoutDurationMinutes")]
    [SettingsRange(5, 240, "minutes")]
    public int PinLoginLockoutDurationMinutes { get; set; } = 15;

    public void InitializeConfigSection(bool isDevelopment)
    {
        // Use only in development
        if (isDevelopment)
        {
            PasswordComplexitySettings = new PasswordComplexitySettings
            {
                RequireDigit = Password.DefaultRequireDigit,
                RequireNonAlphanumeric = Password.DefaultRequireNonAlphanumeric,
                RequireBothUpperAndLowerCase = Password.DefaultRequireBothUpperAndLowerCase,
                RequiredLength = Password.DefaultMinimumLength,
                RequireNoUsernameInPassword = Password.RequireNoUsernameInPassword,
            };
            PasswordExpirationEnabled = true;
            PasswordExpirationDays = 90;
            EnablePinLogin = false;
        }
    }
}
