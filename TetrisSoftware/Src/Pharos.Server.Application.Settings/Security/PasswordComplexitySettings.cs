using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Security;

public class PasswordComplexitySettings
{
    /// <summary>
    /// The required length of the password.
    /// </summary>
    [SettingsRange(Password.DefaultMinimumLength, Password.DefaultMaximumLength)]
    [DisplayNameLocal("PasswordComplexitySettings_RequiredLength")]
    public int RequiredLength { get; set; } = Password.DefaultMinimumLength;

    /// <summary>
    /// Indicates whether an uppercase letter is required in the password.
    /// </summary>
    [DisplayNameLocal("PasswordComplexitySettings_RequireBothUpperAndLowerCase")]
    public bool RequireBothUpperAndLowerCase { get; set; } = Password.DefaultRequireBothUpperAndLowerCase;

    /// <summary>
    /// Indicates whether a digit is required in the password.
    /// </summary>
    [DisplayNameLocal("PasswordComplexitySettings_RequireDigit")]
    public bool RequireDigit { get; set; } = Password.DefaultRequireDigit;

    /// <summary>
    /// Indicates whether a non-alphanumeric character is required in the password.
    /// </summary>
    [DisplayNameLocal("PasswordComplexitySettings_RequireNonAlphanumeric")]
    public bool RequireNonAlphanumeric { get; set; } = Password.DefaultRequireNonAlphanumeric;

    /// <summary>
    /// The max length of the password.
    /// </summary>
    [DisplayNameLocal("PasswordComplexitySettings_RequireNoUsernameInPassword")]
    public bool RequireNoUsernameInPassword { get; set; } = Password.RequireNoUsernameInPassword;
}
