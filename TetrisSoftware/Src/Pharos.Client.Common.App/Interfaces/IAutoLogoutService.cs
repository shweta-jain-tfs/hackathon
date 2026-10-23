namespace Pharos.Client.Common.App.Interfaces;

public interface IAutoLogoutService
{
    /// <summary>
    /// Starts monitoring once. Concurrent callers await the same initialization.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Restarts the inactivity timeout after a user action, including an action to dismiss the idle prompt.
    /// </summary>
    void StayLoggedIn();

    /// <summary>
    /// Raised when the user enters the final idle period before automatic logout.
    /// The parameter is the number of seconds remaining. Show the stay-logged-in warning.
    /// Call StayLoggedIn after a user action to reset the timeout. This event does not fire on each tick.
    /// </summary>
    event Func<int, Task>? UserIdle;

    /// <summary>
    /// Raised when the idle period ends. Close the stay-logged-in warning.
    /// This includes screen or compartment activity, settings changes, logout, and disposal.
    /// </summary>
    event Func<Task>? UserActive;
}
