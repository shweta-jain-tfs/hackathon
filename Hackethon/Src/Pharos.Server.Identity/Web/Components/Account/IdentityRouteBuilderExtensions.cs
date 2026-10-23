using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Infrastructure;

namespace Pharos.Server.Identity.Web.Components.Account;

/// <summary>
/// The request type for the "/login" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi{TUser}(IEndpointRouteBuilder)"/>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// The user's email address which acts as a username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// The user's password.
    /// </summary>
    public required string Password { get; init; }
}

internal static class IdentityRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/Account");

        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<ApplicationUser> signInManager,
            ICurrentInstrumentService currentInstrumentService,
            IPublisher publisher,
            [FromForm] string returnUrl) =>
            await LogoutAsync(signInManager, currentInstrumentService, publisher, returnUrl));

        // Allow WebAssembly client to log out without Antiforgery token
        accountGroup.MapGet("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<ApplicationUser> signInManager,
            ICurrentInstrumentService currentInstrumentService,
            IPublisher publisher,
            [FromQuery] string? returnUrl) =>
            await LogoutAsync(signInManager, currentInstrumentService, publisher, returnUrl));

        var timeProvider = endpoints.ServiceProvider.GetRequiredService<TimeProvider>();
        var bearerTokenOptions = endpoints.ServiceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();

        var apiLoginGroup = endpoints.MapGroup("");
        apiLoginGroup.MapPost("/login", async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>>
            ([FromBody] LoginRequest login, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<ApplicationUser>>();
            var securitySettings = sp.GetRequiredService<IOptionsSnapshot<SecuritySettings>>().Value;
            var userManager = sp.GetRequiredService<Infrastructure.Authorization.IApplicationUserManager>();
            var dateTime = sp.GetRequiredService<IDateTime>();

            var useCookieScheme = (useCookies == true) || (useSessionCookies == true);
            var isPersistent = (useCookies == true) && (useSessionCookies != true);
            signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

            // Check password expiration before completing sign-in
            var user = await userManager.FindByNameAsync(login.Username);
            if (user != null)
            {
                var passwordExpired = user.TemporaryPassword
                    || PasswordExpirationEvaluator.IsPasswordExpired(user.PasswordEmittedDateTimeOffset, securitySettings, dateTime.Now);
                if (passwordExpired)
                {
                    return TypedResults.Problem("Password change required.", statusCode: StatusCodes.Status401Unauthorized);
                }
            }

            var result = await signInManager.PasswordSignInAsync(login.Username, login.Password, isPersistent, lockoutOnFailure: securitySettings.EnablePasswordLoginLockout);

            if (!result.Succeeded)
            {
                return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
            }

            // The signInManager already produced the needed response in the form of a cookie or bearer token.
            return TypedResults.Empty;
        });

        apiLoginGroup.MapPost("/refresh", async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
            ([FromBody] RefreshRequest refreshRequest, [FromServices] IServiceProvider sp) =>
        {
            var signInManager = sp.GetRequiredService<SignInManager<ApplicationUser>>();
            var refreshTokenProtector = bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
            var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

            // Reject the /refresh attempt with a 401 if the token expired or the security stamp validation fails
            if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
                timeProvider.GetUtcNow() >= expiresUtc ||
                await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not { } user)

            {
                return TypedResults.Challenge();
            }

            var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
            return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
        });

#if DEBUG
        apiLoginGroup.MapPost("/register", async Task<Results<Ok, InternalServerError>>
            ([FromBody] LoginRequest login, [FromServices] IServiceProvider sp) =>
        {
            var userStore = sp.GetRequiredService<IUserStore<ApplicationUser>>();
            var userManager = sp.GetRequiredService<Infrastructure.Authorization.IApplicationUserManager>();

            var user = new ApplicationUser
            {
                LastLoginDateTimeOffset = DateTime.Now,
                TemporaryPassword = false,
            };

            await userStore.SetUserNameAsync(user, login.Username, CancellationToken.None);
            var result = await userManager.CreateAsync(user, login.Password);

            if (!result.Succeeded)
            {
                return TypedResults.InternalServerError();
            }

            await userManager.AddToRoleAsync(user, Contracts.Security.BuiltInRoles.RndUser);

            return TypedResults.Ok();
        });
#endif

        return accountGroup;
    }

    /// <summary>
    /// Signs the current user out, publishes a <see cref="UserLoggedOutEvent"/>, and redirects to <paramref name="returnUrl"/>.
    /// </summary>
    private static async Task<IResult> LogoutAsync(
        SignInManager<ApplicationUser> signInManager,
        ICurrentInstrumentService currentInstrumentService,
        IPublisher publisher,
        string? returnUrl)
    {
        var instrumentId = currentInstrumentService.InstrumentId;
        await signInManager.SignOutAsync();
        await publisher.Publish(new UserLoggedOutEvent(instrumentId));
        return TypedResults.LocalRedirect($"~/{returnUrl?.TrimStart('/') ?? ""}");
    }
}
