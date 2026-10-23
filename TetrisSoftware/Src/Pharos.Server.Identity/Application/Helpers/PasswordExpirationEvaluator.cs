using Pharos.Server.Application.Settings.Security;

namespace Pharos.Server.Identity.Application.Helpers;

/// <summary>
/// Evaluates whether a user's password has expired based on the emitted timestamp and current security settings.
/// Expiration is computed dynamically: emitted + PasswordExpirationDays, so changing the setting takes effect immediately.
/// </summary>
internal static class PasswordExpirationEvaluator
{
    /// <summary>
    /// Returns true if the password is expired.
    /// </summary>
    /// <param name="passwordEmittedDateTimeOffset">The timestamp when the current password was created or last changed. Null means unknown (grandfathered — not considered expired).</param>
    /// <param name="securitySettings">Current security settings.</param>
    /// <param name="now">Current date/time (use injected IDateTime.Now for testability).</param>
    public static bool IsPasswordExpired(
        DateTimeOffset? passwordEmittedDateTimeOffset,
        SecuritySettings securitySettings,
        DateTimeOffset now)
    {
        if (!securitySettings.PasswordExpirationEnabled)
        {
            return false;
        }

        if (passwordEmittedDateTimeOffset == null)
        {
            // Legacy policy: null emitted timestamp → grandfathered, not expired.
            return false;
        }

        return now >= passwordEmittedDateTimeOffset.Value.AddDays(securitySettings.PasswordExpirationDays);
    }
}
