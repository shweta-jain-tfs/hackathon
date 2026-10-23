namespace Pharos.Contracts.Security;

/// <summary>
/// The list of permissions must be synchronized with the "Pharos User Access Permissions" document (DocID 809463).
/// </summary>
public static class Password
{
#if DEBUG
    //Relax Password requirements in development
    public const int DefaultMinimumLength = 1;
    public const bool DefaultRequireDigit = false;
    public const bool DefaultRequireNonAlphanumeric = false;
    public const bool DefaultRequireBothUpperAndLowerCase = false;
#else
    public const int DefaultMinimumLength = 8;
    public const bool DefaultRequireDigit = true;
    public const bool DefaultRequireNonAlphanumeric = true;
    public const bool DefaultRequireBothUpperAndLowerCase = true;
#endif
    public const bool RequireNoUsernameInPassword = false;
    public const int DefaultMaximumLength = 32;

}
