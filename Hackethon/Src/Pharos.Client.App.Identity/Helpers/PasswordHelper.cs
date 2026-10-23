namespace Pharos.Client.App.Identity.Helpers;

public static class PasswordHelper
{
    public static bool IsPasswordValid(string password, UiSettings? uiSettings, string username)
    {
        // Note! else logic invert youre brain
        if (string.IsNullOrEmpty(password)
            || uiSettings == null
            || (uiSettings.SecuritySettings.PasswordRequireDigit && !password.Any(char.IsDigit))
            || (uiSettings.SecuritySettings.PasswordRequiresBothUpperAndLowerCase
                && (!password.Any(char.IsUpper) || !password.Any(char.IsLower)))
            || (uiSettings.SecuritySettings.PasswordRequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
            || uiSettings.SecuritySettings.PasswordRequiredLength > password.Length
            || (uiSettings.SecuritySettings.PasswordRequireNoUsername
                && password.Contains(username, StringComparison.OrdinalIgnoreCase))
            )
        {
            return false;
        }
        return true;
    }

    public static List<string> PasswordComplexityGuiList(UiSettings? uiSettings, IStringLocalizer<Translations>? Loc)
    {
        ArgumentNullException.ThrowIfNull(uiSettings);

        ArgumentNullException.ThrowIfNull(Loc);

        return new List<string>
            {
                uiSettings.SecuritySettings.PasswordRequireDigit ? Loc["ApplicationUsersPage_AddDialog_PasswordDefaultRequireDigit"] : "",
                uiSettings.SecuritySettings.PasswordRequiresBothUpperAndLowerCase ? Loc["ApplicationUsersPage_AddDialog_PasswordDefaultRequireBothLowerAndUpperCase"] : "",
                uiSettings.SecuritySettings.PasswordRequireNonAlphanumeric ? Loc["ApplicationUsersPage_AddDialog_PasswordDefaultRequireNonAlphanumeric"] : "",
                uiSettings.SecuritySettings.PasswordRequiredLength > 0 ? $"{ Loc["ApplicationUsersPage_AddDialog_PasswordDefaultMinimumLength"]}: {uiSettings.SecuritySettings.PasswordRequiredLength}" : "",
                uiSettings.SecuritySettings.PasswordRequireNoUsername ? Loc["ApplicationUsersPage_AddDialog_PasswordRequireNoUsername"] : "",
            }.Where(item => !string.IsNullOrEmpty(item)).ToList();
    }
}
