using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Application.Commands;
using Pharos.Server.Identity.Application.Helpers;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public partial class Login : ComponentBase
{
    #region Parameters and form fields

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromForm]
    public string UserName { get; set; } = null!;

    [SupplyParameterFromForm]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [SupplyParameterFromForm]
    public bool RememberMe { get; set; }

    #endregion

    #region Services

    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = null!;
    [Inject] private IIdentityRedirectManager RedirectManager { get; set; } = null!;
    [Inject] private IChangePasswordTicketService ChangePasswordTicketService { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] ICurrentInstrumentService CurrentInstrumentService { get; set; } = null!;
    [Inject] private IPinLockoutService PinLockoutService { get; set; } = null!;
    [Inject] private IDateTime DateTime { get; set; } = null!;
    [Inject] private IOptionsSnapshot<SecuritySettings> SecuritySettings { get; set; } = null!;
    [Inject] private ILogger<Login> Logger { get; set; } = null!;

    #endregion

    private bool _usernameEmpty;
    private bool _passwordEmpty;
    private string? _errorMessage;
    private bool _isPinModeEnabled;
    private bool _isPinLoginAvailable;

    private bool LoginFailed => _errorMessage != null;
    private bool ShouldFocusPassword => LoginFailed && _passwordEmpty && !_usernameEmpty;

    protected override async Task OnInitializedAsync()
    {
        await RedirectManager.RedirectToRegisterIfNotInitialized(ReturnUrl);

        _isPinModeEnabled = PinLockoutService.IsPinModeEnabled();
        _isPinLoginAvailable = !await PinLockoutService.IsPinLoginSuspended();
    }

    public async Task OnSubmit()
    {
        if (!ValidateInput())
        {
            return;
        }

        try
        {
            await DoLogin();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An unexpected error occurred during login.");
            _errorMessage = Translations.ErrorUnexpected;
        }
    }

    private async Task DoLogin()
    {
        var user = await UserManager.FindByNameAsync(UserName);
        if (user == null)
        {
            Logger.LogInformation("Login attempt failed. User not found.");
            _errorMessage = Translations.Login_InvalidUserNameOrPassword;
            Password = string.Empty;
            _passwordEmpty = true;
            return;
        }

        // check the password but not sign in yet
        var lockoutOnFailure = SecuritySettings.Value.EnablePasswordLoginLockout;
        var signInResult = await SignInManager.CheckPasswordSignInAsync(user, Password, lockoutOnFailure: lockoutOnFailure);
        if (!signInResult.Succeeded)
        {
            Logger.LogInformation("Login attempt failed. User ID={UserId}. Active: {IsActive} Lockout: {IsLockedOut}", user.Id, user.IsActive, signInResult.IsLockedOut);
            _errorMessage = signInResult switch
            {
                { IsLockedOut: true } => Translations.Login_UserLockedout,
                { IsNotAllowed: true } => Translations.Login_UserIsInactive,
                _ => Translations.Login_InvalidUserNameOrPassword
            };
            Password = string.Empty;
            _passwordEmpty = true;
            return;
        }

        // If the user is a service engineer, validate their permissions before allowing them to log in
        // TODO: placement of this code must be reconsidered when we implement proper service login.
        var isServiceEngineer = (await UserManager.GetRolesAsync(user)).Any(x => x == BuiltInRoles.ServiceEngineer);
        if (isServiceEngineer)
        {
            await Mediator.Send(new ValidateServiceEngineerPermissionsCommand());
        }

        // check if the password change is required before sign in, if required, redirect to change password page with an access ticket
        var passwordExpired = PasswordExpirationEvaluator.IsPasswordExpired(
            user.PasswordEmittedDateTimeOffset,
            SecuritySettings.Value,
            DateTime.Now);

        var passwordComplexityNotFulfilled = !UserManager.IsPasswordValid(Password, UserName);

        if (user.TemporaryPassword || passwordComplexityNotFulfilled || passwordExpired)
        {
            Logger.LogInformation("Password reset needed for the user ID={UserId}.", user.Id);
            var ticket = ChangePasswordTicketService.CreateTicket(UserName, ChangePasswordTicketType.Initial);
            RedirectManager.RedirectTo($"{AccountRoutes.ChangePassword}{UserName}?ticket={Uri.EscapeDataString(ticket)}&returnUrl={Uri.EscapeDataString(ReturnUrl ?? "")}");
            return;
        }

        // sign in the user
        await SignInManager.SignInAsync(user, isPersistent: RememberMe);
        user.LastLoginDateTimeOffset = DateTime.Now;
        var updateResult = await UserManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, updateResult.Errors.Select(e => e.Description)));
        }

        var instrumentNumber = CurrentInstrumentService.InstrumentNumber;

        await Mediator.Publish(new UserLoggedInEvent(user.Id, instrumentNumber));

        Logger.LogInformation("Login successful. User ID={UserId}.", user.Id);

        if (PinLockoutService.IsPinModeEnabled() && user.NeedsNewPin)
        {
            RedirectManager.RedirectTo(AccountRoutes.PinSetup);
        }
        else
        {
            RedirectManager.RedirectTo(ReturnUrl);
        }
    }

    private bool ValidateInput()
    {
        _usernameEmpty = string.IsNullOrWhiteSpace(UserName);
        _passwordEmpty = string.IsNullOrWhiteSpace(Password);

        if (_usernameEmpty || _passwordEmpty)
        {
            _errorMessage = Translations.Login_RequiredFieldEmpty;
            return false;
        }

        return true;
    }
}

