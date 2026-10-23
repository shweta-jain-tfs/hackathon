using Pharos.Contracts.Security;

namespace Pharos.Client.Common.App.Interfaces;

public interface IAuthorizationService
{
    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permission">
    /// The name of the permission to check. See <see cref="Permissions"/> for the list of these.
    /// </param>
    /// <returns>True if the current user has the required permission, otherwise, false.</returns>
    Task<bool> IsAuthorized(string permission);

    /// <summary>
    /// Checks if the current user the service role.
    /// </summary>
    Task<bool> IsServiceRole();


    /// <summary>
    /// Gets the value indicating whether the RnD mode is enabled in Pharos.
    /// </summary>
    Task<bool> IsRndMode();
}