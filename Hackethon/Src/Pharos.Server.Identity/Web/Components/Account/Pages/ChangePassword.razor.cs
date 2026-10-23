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
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;
namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public partial class ChangePassword : ComponentBase
{
    #region Parameters and form fields

    [Parameter]
    public required string Username { get; set; }

    [SupplyParameterFromQuery]
    public string? Ticket { get; set; }

    [SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    [SupplyParameterFromForm]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [SupplyParameterFromForm]
    [DataType(DataType.Password)]
    public string ConfirmationPassword { get; set; } = null!;

    [SupplyParameterFromForm]
    public string? RetryTicket { get; set; }

    #endregion

    #region Services
    [Inject] private IIdentityRedirectManager RedirectManager { get; set; } = null!;
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = null!;
    [Inject] private IDateTime DateTime { get; set; } = null!;
    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private IChangePasswordTicketService TicketService { get; set; } = null!;
    [Inject] private ILogger<ChangePassword> Logger { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] private IOptionsSnapshot<SecuritySettings> SecuritySettings { get; set; } = null!;
    [Inject] ICurrentInstrumentService CurrentInstrumentService { get; set; } = null!;

    #endregion

    private bool _newPasswordHasError;
    private bool _confirmationPasswordHasError;
    private string? _errorMessage;

    private bool ChangeFailed => _errorMessage != null;

    public async Task OnSubmit()
    {
        if (!ValidateInput())
        {
            return;
        }

        try
        {
            await DoChangePassword();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An unexpected error occurred during password change.");
            _errorMessage = Translations.ErrorUnexpected;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await RedirectManager.RedirectToRegisterIfNotInitialized(ReturnUrl);

        if ((!string.IsNullOrWhiteSpace(RetryTicket) && TicketService.TryConsumeTicket(Username, RetryTicket, ChangePasswordTicketType.Retry))
            || (!string.IsNullOrWhiteSpace(Ticket) && TicketService.TryConsumeTicket(Username, Ticket, ChangePasswordTicketType.Initial)))
        {
            // access to the form is granted
            RetryTicket = TicketService.CreateTicket(Username, ChangePasswordTicketType.Retry);
            return;
        }

        Logger.LogInformation("A ticket for changing the password is missing, invalid, or expired.");

        RedirectManager.RedirectTo(AccountRoutes.Login);
    }

    private async Task DoChangePassword()
    {
        var user = await UserManager.FindByNameAsync(Username);
        if (user == null)
        {
            Logger.LogError("User not found.");
            RedirectManager.RedirectTo(AccountRoutes.Login);
            return;
        }

        if (!UserManager.IsPasswordValid(NewPassword, Username))
        {
            _errorMessage = Translations.ChangePassword_PasswordComplexityError;
            _newPasswordHasError = true;
            _confirmationPasswordHasError = true;
            return;
        }

        if ((await SignInManager.CheckPasswordSignInAsync(user, NewPassword, lockoutOnFailure: false)).Succeeded)
        {
            _errorMessage = Translations.ChangePassword_PasswordsAreTheSame;
            NewPassword = "";
            ConfirmationPassword = "";
            return;
        }

        var token = await UserManager.GeneratePasswordResetTokenAsync(user);
        var result = await UserManager.ResetPasswordAsync(user, token, NewPassword);
        if (!result.Succeeded)
        {
            _errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            return;
        }

        Logger.LogInformation("Temporary password changed. UserId: {UserId}", user.Id);

        user.TemporaryPassword = false;

        user.PasswordEmittedDateTimeOffset = DateTime.Now;

        // sign in the user
        await SignInManager.SignInAsync(user, false);
        user.LastLoginDateTimeOffset = DateTime.Now;
        var updateResult = await UserManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, updateResult.Errors.Select(e => e.Description)));
        }

        var instrumentNumber = CurrentInstrumentService.InstrumentNumber;

        await Mediator.Publish(new UserLoggedInEvent(user.Id, instrumentNumber));

        Logger.LogInformation("Login successful. User ID={UserId}.", user.Id);

        if (SecuritySettings.Value.EnablePinLogin && user.NeedsNewPin)
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
        _newPasswordHasError = string.IsNullOrWhiteSpace(NewPassword);
        _confirmationPasswordHasError = string.IsNullOrWhiteSpace(ConfirmationPassword);

        if (_newPasswordHasError || _confirmationPasswordHasError)
        {
            _errorMessage = Translations.PasswordRequired;
            return false;
        }

        if (NewPassword != ConfirmationPassword)
        {
            _errorMessage = Translations.ChangePassword_PasswordsDoNotMatch;
            NewPassword = "";
            ConfirmationPassword = "";
            return false;
        }

        return true;
    }
}

