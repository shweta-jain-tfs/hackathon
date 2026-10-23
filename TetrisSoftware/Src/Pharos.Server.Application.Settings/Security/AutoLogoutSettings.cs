using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Security;

public class AutoLogoutSettings
{
    /// <summary>
    /// Indicates whether users are automatically logged out after a period of inactivity.
    /// </summary>
    [DisplayNameLocal("SecuritySettings_AutoLogoutEnabled")]
    public bool AutoLogoutEnabled { get; set; } = true;

    /// <summary>
    /// The inactivity time (in minutes) before the user is automatically logged out.
    /// Valid range: 1-120 minutes. Default: 10 minutes.
    /// </summary>
    [DependsFrom(nameof(AutoLogoutEnabled), "True", DependencyType.Hide)]
    [DisplayNameLocal("SecuritySettings_AutoLogoutTimeoutMinutes")]
    [SettingsRange(1, 120, "minutes")]
    public int AutoLogoutTimeoutMinutes { get; set; } = 10;
}
