namespace Pharos.Server.Identity.Infrastructure.Authentication;

/// <summary>
/// Service for managing PIN login lockout state per instrument/device.
/// Tracks consecutive failed PIN attempts and implements lockout after 3 failures.
/// This is a scoped service that automatically uses the current instrument context.
/// </summary>
public interface IPinLockoutService
{
    /// <summary>
    /// Checks whether PIN mode is enabled and current instrument context is available.
    /// </summary>
    /// <returns>True if PIN login is enabled for the current context, false otherwise.</returns>
    bool IsPinModeEnabled();

    /// <summary>
    /// Checks if PIN login is currently suspended for the current instrument context.
    /// </summary>
    /// <returns>True if PIN login is suspended, false otherwise.</returns>
    Task<bool> IsPinLoginSuspended();

    /// <summary>
    /// Records a failed PIN login attempt for the current instrument.
    /// If the attempt count reaches 3, the instrument will be locked out for the configured duration.
    /// </summary>
    /// <returns>True if this attempt triggered a lockout (count reached 3), false otherwise.</returns>
    Task<bool> RecordFailedAttempt();

    /// <summary>
    /// Resets failed PIN login attempts for the current instrument after successful login.
    /// </summary>
    Task ResetAttempts();
}
