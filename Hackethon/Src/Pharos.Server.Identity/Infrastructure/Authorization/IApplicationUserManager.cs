using Microsoft.AspNetCore.Identity;
using Pharos.Server.Identity.Domain;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

/// <summary>
/// Interface for managing application users and their roles, passwords, and other related operations.
/// </summary>
public interface IApplicationUserManager
{
    /// <summary>
    /// Validates the provided password against the application's password complexity rules.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="userName">The username associated with the password.</param>
    /// <returns>True if the password meets the complexity requirements; otherwise, false.</returns>
    bool IsPasswordValid(string? password, string userName);

    /// <summary>
    /// Gets a list of password complexity rules as displayable strings.
    /// </summary>
    List<string> PasswordComplexityDisplayList { get; }

    /// <summary>
    /// Retrieves all application users.
    /// </summary>
    /// <returns>A list of all application users.</returns>
    Task<List<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Adds the specified user to a role.
    /// </summary>
    /// <param name="user">The user to add to the role.</param>
    /// <param name="role">The name of the role.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role);

    /// <summary>
    /// Creates a new user with the specified password.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="password">The password for the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);

    /// <summary>
    /// Finds a user by their unique identifier as a string.
    /// </summary>
    /// <param name="userId">The unique identifier of the user as a string.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    Task<ApplicationUser?> FindByIdAsync(string userId);

    /// <summary>
    /// Finds a user by their unique identifier as a <see cref="Guid"/>.
    /// </summary>
    /// <param name="userId">The unique identifier of the user as a <see cref="Guid"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    Task<ApplicationUser?> FindByGuidAsync(Guid userId);

    /// <summary>
    /// Finds a user by their username.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    Task<ApplicationUser?> FindByNameAsync(string userName);

    /// <summary>
    /// Gets the roles assigned to the specified user.
    /// </summary>
    /// <param name="user">The user whose roles are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of role names.</returns>
    Task<IList<string>> GetRolesAsync(ApplicationUser user);

    /// <summary>
    /// Gets the users assigned to a specific role.
    /// </summary>
    /// <param name="role">The name of the role.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of users in the role.</returns>
    Task<IList<ApplicationUser>> GetUsersInRoleAsync(string role);

    /// <summary>
    /// Removes the specified user from the given roles.
    /// </summary>
    /// <param name="user">The user to remove from the roles.</param>
    /// <param name="roles">The roles to remove the user from.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>
    /// Checks if the specified user is in a specific role.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <param name="role">The name of the role.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the user is in the role, otherwise false.</returns>
    Task<bool> IsInRoleAsync(ApplicationUser user, string role);

    /// <summary>
    /// Updates the specified user's information.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> UpdateAsync(ApplicationUser user);

    /// <summary>
    /// Deletes the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Generates a password reset token for the specified <paramref name="user"/>, using
    /// the configured password reset token provider.
    /// </summary>
    /// <param name="user">The user to generate a password reset token for.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation,
    /// containing a password reset token for the specified <paramref name="user"/>.</returns>
    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);

    /// <summary>
    /// Resets the password for the specified user using the provided token and new password.
    /// </summary>
    /// <param name="user">The user whose password is to be reset.</param>
    /// <param name="token">The password reset token.</param>
    /// <param name="newPassword">The new password for the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);

    /// <summary>
    /// Checks whether the provided password matches the stored password for the specified user.
    /// </summary>
    /// <param name="user">The user to validate.</param>
    /// <param name="password">The plaintext password to check.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is true when password is valid.</returns>
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    /// <summary>
    /// Updates the specified user with a temporary password.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="temporaryPassword">The temporary password to set for the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> UpdateUserWithTemporaryPasswordAsync(Guid userId, string temporaryPassword);

    /// <summary>
    /// Checks if the users in the application have been initialized.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if users are initialized, otherwise false.</returns>
    Task<bool> AreUsersInitialized();

    /// <summary>
    /// Sets or updates the PIN for the specified user.
    /// </summary>
    /// <param name="user">The user to set the PIN for.</param>
    /// <param name="pin">The PIN as a string with exactly four digits.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result of the operation.</returns>
    Task<IdentityResult> SetPinAsync(ApplicationUser user, string pin);

    /// <summary>
    /// Finds a user by their PIN code.
    /// </summary>
    /// <param name="pin">The PIN to search for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user if found, otherwise null.</returns>
    Task<ApplicationUser?> FindByPinAsync(string pin);

    /// <summary>
    /// Checks if the specified user is active.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the user is active, otherwise false.</returns>
    Task<bool> IsActive(Guid userId);
}
