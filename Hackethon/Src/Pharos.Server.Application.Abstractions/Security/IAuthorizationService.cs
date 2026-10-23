using Pharos.Contracts.Security;

namespace Pharos.Server.Application.Security;

/// <summary>
/// User authorization service.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if the current user (the one that has started the HTTP request) has the specified permissions.
    /// </summary>
    /// <param name="permissions">
    /// The list of permissions to check. See <see cref="Permissions"/> for the list of these.
    /// </param>
    /// <returns>True if the user account is active and has all required permissions, otherwise, false.</returns>
    Task<bool> AuthorizeCurrentUserAsync(params string[] permissions);

    /// <summary>
    /// Checks if the user has the specified permissions.
    /// </summary>
    /// <param name="userId">
    /// The ID of the user to whose permissions needs to be checked.
    /// </param>
    /// <param name="permissions">
    /// The list of permissions to check. See <see cref="Permissions"/> for the list of these.
    /// </param>
    /// <returns>True if the user account is active and has all required permissions, otherwise, false.</returns>
    Task<bool> AuthorizeUserAsync(Guid userId, params string[] permissions);

    /// <summary>
    /// Checks if the current user (the one that has started the HTTP request) has the specified role.
    /// </summary>
    /// <param name="role">
    /// The name of the role to check.
    /// </param>
    /// <returns>True if the user account is active and has the required role, otherwise, false.</returns>
    Task<bool> IsCurrentUserInRoleAsync(string role);

    /// <summary>
    /// Gets the value indicating whether the RnD mode is enabled in Pharos.
    /// </summary>
    bool RndMode { get; }

    /// <summary>
    /// Gets the value indicating whether the RUO (research-use only) mode is enabled in Pharos.
    /// </summary>
    bool RuoMode { get; }
}
