using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Infrastructure.Authentication;

public class CustomSignInManager(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation,
    IApplicationUserManager applicationUserManager,
    IPinLockoutService pinLockoutService,
    IOptionsSnapshot<SecuritySettings> securitySettings)
    : SignInManager<ApplicationUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    public override async Task<SignInResult> CheckPasswordSignInAsync(ApplicationUser user, string password, bool lockoutOnFailure)
    {
        // need this check here to cover the API login endpoint
        if (!user.IsActive)
        {
            return SignInResult.NotAllowed;
        }

        var currentSecuritySettings = securitySettings.Value;

        UserManager.Options.Lockout.MaxFailedAccessAttempts = currentSecuritySettings.PasswordLoginLockoutMaxFailedAttempts;
        UserManager.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(currentSecuritySettings.PasswordLoginLockoutDurationMinutes);

        var isPasswordLockoutEnabled = currentSecuritySettings.EnablePasswordLoginLockout;
        if (user.LockoutEnabled != isPasswordLockoutEnabled)
        {
            user.LockoutEnabled = isPasswordLockoutEnabled;
        }

        return await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
    }

    /// <summary>
    /// Standalone PIN authentication - no username required.
    /// Returns the authenticated user for further processing.
    /// Implements PIN login lockout after 3 consecutive failed attempts per instrument.
    /// </summary>
    /// <param name="pin">The PIN to authenticate with.</param>
    /// <param name="isPersistent"></param>
    public async Task<(SignInResult Result, ApplicationUser? User)> PinSignInWithUserAsync(string pin, bool isPersistent = false)
    {
        // Check if PIN login is enabled for current instrument context
        if (!pinLockoutService.IsPinModeEnabled())
        {
            return (SignInResult.NotAllowed, null);
        }

        // Check if PIN login is suspended from failed attempts
        if (await pinLockoutService.IsPinLoginSuspended())
        {
            return (SignInResult.LockedOut, null);
        }

        var user = await applicationUserManager.FindByPinAsync(pin);
        if (user == null)
        {
            return (SignInResult.Failed, null);
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return (SignInResult.NotAllowed, null);
        }

        // Check if user is locked out (account-level lockout, not PIN lockout)
        if (await UserManager.IsLockedOutAsync(user))
        {
            return (SignInResult.LockedOut, null);
        }

        await UserManager.ResetAccessFailedCountAsync(user);

        // Sign in the user
        await base.SignInAsync(user, isPersistent, authenticationMethod: "Pin");

        // Reset PIN failure attempts for this instrument on successful login
        await pinLockoutService.ResetAttempts();

        return (SignInResult.Success, user);
    }

    /// <summary>
    /// Handles PIN login failure by recording a failed attempt for the current instrument.
    /// After 3 consecutive failures, the instrument will be locked out for the configured duration.
    /// </summary>
    /// <returns>True if this attempt triggered a lockout, false otherwise.</returns>
    public async Task<bool> HandlePinLoginFailureAsync()
    {
        return await pinLockoutService.RecordFailedAttempt();
    }

    public override Task SignInWithClaimsAsync(ApplicationUser user, AuthenticationProperties? authenticationProperties, IEnumerable<Claim> additionalClaims)
    {
        // Ensure GivenName claim is included
        var displayName = !string.IsNullOrEmpty(user.FullName) ? user.FullName : user.UserName ?? string.Empty;

        // Create a new list of claims including the GivenName claim
        var additionalClaimsList = additionalClaims;
        additionalClaimsList = additionalClaimsList.Append(new Claim(ClaimTypes.GivenName, displayName));

        return base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaimsList);
    }
}