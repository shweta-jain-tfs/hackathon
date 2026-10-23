using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Infrastructure.Authorization;
/// <summary>
/// UserManager(ApplicationUser) wrapper class
/// </summary>
internal class ApplicationUserManager : IApplicationUserManager
{
    private const string _activeUsersCacheKey = "Identity.ActiveUsers";
    private static readonly TimeSpan _activeUsersCacheDuration = TimeSpan.FromMinutes(15);

    private readonly UserManager<ApplicationUser> _identityUserManager;
    private readonly IDateTime _dateTime;
    private readonly IMemoryCache _memoryCache;
    private readonly PasswordHasher<ApplicationUser> _pinHasher;
    private bool RequireNoUsernameInPassword { get; set; }

    public ApplicationUserManager(UserManager<ApplicationUser> identityUserManager, IOptionsSnapshot<SecuritySettings> identitySettings, IDateTime dateTime, IMemoryCache memoryCache)
    {
        _identityUserManager = identityUserManager;
        _dateTime = dateTime;
        _memoryCache = memoryCache;
        _pinHasher = new PasswordHasher<ApplicationUser>();
        var passwordComplexitySettings = identitySettings.Value.PasswordComplexitySettings;
        RequireNoUsernameInPassword = passwordComplexitySettings.RequireNoUsernameInPassword;
        SyncSettingsToManager(passwordComplexitySettings);
    }
    /// <summary>
    /// Adding current settings to manager password options
    /// </summary>
    /// <param name="passwordSettings"></param>
    private void SyncSettingsToManager(PasswordComplexitySettings passwordSettings)
    {
        var userManagerPasswordOptions = _identityUserManager.Options.Password;

        userManagerPasswordOptions.RequireDigit = passwordSettings.RequireDigit;
        userManagerPasswordOptions.RequireNonAlphanumeric = passwordSettings.RequireNonAlphanumeric;
        userManagerPasswordOptions.RequireUppercase = passwordSettings.RequireBothUpperAndLowerCase;
        userManagerPasswordOptions.RequireLowercase = passwordSettings.RequireBothUpperAndLowerCase;
        userManagerPasswordOptions.RequiredLength = passwordSettings.RequiredLength;
    }

    public bool IsPasswordValid(string? password, string userName)
    {
        // Note! else logic invert your brain
        if (password == null ||
            (_identityUserManager.Options.Password.RequireDigit && !password.Any(char.IsDigit)) ||
            (_identityUserManager.Options.Password.RequireLowercase && !password.Any(char.IsLower)) ||
            (_identityUserManager.Options.Password.RequireUppercase && !password.Any(char.IsUpper)) ||
            (_identityUserManager.Options.Password.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit)) ||
            (_identityUserManager.Options.Password.RequiredLength > password.Length) ||
            (RequireNoUsernameInPassword && password.Contains(userName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        return true;
    }

    public List<string> PasswordComplexityDisplayList => new List<string>
            {
                _identityUserManager.Options.Password.RequireDigit ? Translations.ChangePassword_PasswordDefaultRequireDigit : "",
                _identityUserManager.Options.Password.RequireLowercase ? Translations.ChangePassword_PasswordDefaultRequireLowercase : "",
                _identityUserManager.Options.Password.RequireUppercase ? Translations.ChangePassword_PasswordDefaultRequireUppercase : "",
                _identityUserManager.Options.Password.RequireNonAlphanumeric ? Translations.ChangePassword_PasswordDefaultRequireNonAlphanumeric : "",
                _identityUserManager.Options.Password.RequiredLength > 0 ? $"{Translations.ChangePassword_PasswordDefaultMinimumLength}: {_identityUserManager.Options.Password.RequiredLength}" : "",
                RequireNoUsernameInPassword ? Translations.ChangePassword_PasswordRequireNoUsername : ""
            }.Where(item => !string.IsNullOrEmpty(item)).ToList();

    public Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role)
    {
        return _identityUserManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)

    {
        if (user.UserName == null
            || user.UserName.Length > UserContract.MaximumNameLength
            || !IsPasswordValid(password, user.UserName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordComplexityViolation",
                Description = Translations.ChangePassword_PasswordComplexityViolation
            });
        }
        var result = await _identityUserManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            user.PasswordEmittedDateTimeOffset = _dateTime.Now;
            await _identityUserManager.UpdateAsync(user);
            InvalidateActiveUsersCache();
        }

        return result;
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId)
    {
        return _identityUserManager.FindByIdAsync(userId);
    }

    public Task<ApplicationUser?> FindByNameAsync(string userName)
    {
        return _identityUserManager.FindByNameAsync(userName);
    }

    public Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return _identityUserManager.Users.ToListAsync();
    }
    public Task<ApplicationUser?> FindByGuidAsync(Guid userId)
    {
        return _identityUserManager.FindByIdAsync(userId.ToString());
    }

    public Task<IList<string>> GetRolesAsync(ApplicationUser user)
    {
        return _identityUserManager.GetRolesAsync(user);
    }

    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
    {
        if (_identityUserManager == null)
        {
            throw new InvalidOperationException("UserManager is not initialized.");
        }

        return _identityUserManager.GetUsersInRoleAsync(roleName);
    }
    public Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        return _identityUserManager.RemoveFromRolesAsync(user, roles);
    }
    public Task<bool> IsInRoleAsync(ApplicationUser user, string role)
    {
        return _identityUserManager.IsInRoleAsync(user, role);
    }
    public async Task<IdentityResult> UpdateAsync(ApplicationUser user)
    {
        var result = await _identityUserManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            InvalidateActiveUsersCache();
        }

        return result;
    }

    public async Task<IdentityResult> DeleteUserAsync(Guid userId)
    {
        var user = _identityUserManager.Users.First(x => x.Id == userId);

        var result = await _identityUserManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            InvalidateActiveUsersCache();
        }
        return result;
    }

    /// <summary>
    /// Generates a password reset token for the specified <paramref name="user"/>, using
    /// the configured password reset token provider.
    /// </summary>
    /// <param name="user">The user to generate a password reset token for.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation,
    /// containing a password reset token for the specified <paramref name="user"/>.</returns>
    public Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
    {
        return _identityUserManager.GeneratePasswordResetTokenAsync(user);
    }

    public Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
    {
        return _identityUserManager.ResetPasswordAsync(user, token, newPassword);
    }

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return _identityUserManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityResult> UpdateUserWithTemporaryPasswordAsync(Guid userId, string temporaryPassword)
    {
        var user = _identityUserManager.Users.First(x => x.Id == userId);
        if (user.UserName == null || !IsPasswordValid(temporaryPassword, user.UserName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordComplexityViolation",
                Description = Translations.ChangePassword_PasswordComplexityViolation
            });
        }

        var token = await _identityUserManager.GeneratePasswordResetTokenAsync(user);
        user.TemporaryPassword = true;

        if (await _identityUserManager.IsLockedOutAsync(user))
        {
            await _identityUserManager.SetLockoutEndDateAsync(user, _dateTime.Now);
        }

        var result = await _identityUserManager.ResetPasswordAsync(user, token, temporaryPassword);
        if (result.Succeeded)
        {
            user.PasswordEmittedDateTimeOffset = _dateTime.Now;
            await _identityUserManager.UpdateAsync(user);
        }
        return result;
    }
    public Task<bool> AreUsersInitialized()
    {
        return _identityUserManager.AreUsersInitialized();
    }

    public async Task<IdentityResult> SetPinAsync(ApplicationUser user, string pin)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Check if user has Service Engineer role
        var isServiceEngineer = await _identityUserManager.IsInRoleAsync(user, BuiltInRoles.ServiceEngineer);
        if (isServiceEngineer)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "ServiceEngineerPinNotAllowed",
                Description = Translations.SetPin_ServiceEngineerPinNotAllowed
            });
        }

        if (!IsPinValid(pin))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidPin",
                Description = Translations.SetPin_InvalidPin
            });
        }

        // Check if PIN is already in use by another user
        // Since PINs are hashed, we need to load all PIN-enabled users and verify
        // Note: If a duplicate PIN is found, we return a generic InvalidPin error
        // instead of DuplicatePin to prevent PIN enumeration attacks
        var usersWithPin = await _identityUserManager.Users
            .Where(u => u.IsPinEnabled && u.Id != user.Id && u.PinHash != null)
            .ToListAsync();

        foreach (var existingUser in usersWithPin)
        {
            if (existingUser.PinHash is null)
            {
                continue;
            }

            var verificationResult = _pinHasher.VerifyHashedPassword(existingUser, existingUser.PinHash, pin);
            if (verificationResult == PasswordVerificationResult.Success ||
                verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "InvalidPin",
                    Description = Translations.SetPin_InvalidPin
                });
            }
        }

        // Hash the PIN before storing
        user.PinHash = _pinHasher.HashPassword(user, pin);
        user.IsPinEnabled = true;

        return await _identityUserManager.UpdateAsync(user);
    }

    public async Task<ApplicationUser?> FindByPinAsync(string pin)
    {
        if (!IsPinValid(pin))
        {
            return null;
        }

        // Since PINs are hashed, we need to load all PIN-enabled users and verify
        var usersWithPin = await _identityUserManager.Users
            .Where(u => u.IsPinEnabled && u.PinHash != null)
            .ToListAsync();

        foreach (var user in usersWithPin)
        {
            if (user.PinHash is null)
            {
                continue;
            }

            var verificationResult = _pinHasher.VerifyHashedPassword(user, user.PinHash, pin);
            if (verificationResult == PasswordVerificationResult.Success ||
                verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return user;
            }
        }

        return null;
    }

    public async Task<bool> IsActive(Guid userId)
    {
        var activeUsers = await GetActiveUsersAsync();
        return activeUsers.Contains(userId);
    }

    private async Task<HashSet<Guid>> GetActiveUsersAsync()
    {
        if (_memoryCache.TryGetValue<HashSet<Guid>>(_activeUsersCacheKey, out var cachedUsers) && cachedUsers is not null)
        {
            return cachedUsers;
        }

        var activeUsers = await _identityUserManager.Users
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();

        var userSet = activeUsers.ToHashSet();
        _memoryCache.Set(_activeUsersCacheKey, userSet, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _activeUsersCacheDuration
        });

        return userSet;
    }

    private void InvalidateActiveUsersCache()
    {
        _memoryCache.Remove(_activeUsersCacheKey);
    }

    private static bool IsPinValid(string? pin)
    {
        // PIN validation: must be exactly 4 digits.
        if (string.IsNullOrEmpty(pin))
        {
            return false;
        }

        // Check if PIN has exact fixed length
        if (pin.Length != ApplicationUser.PinLength)
        {
            return false;
        }

        // Check if PIN contains only digits
        return pin.All(char.IsDigit);
    }
}

