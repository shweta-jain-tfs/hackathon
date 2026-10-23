using System.ComponentModel.DataAnnotations;
using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authentication;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public partial class LoginWithPin : ComponentBase
{
    private string? _errorMessage;

    [Inject] private ILogger<LoginWithPin> Logger { get; set; } = null!;
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = null!;
    [Inject] private IDateTime DateTime { get; set; } = null!;
    [Inject] private IIdentityRedirectManager RedirectManager { get; set; } = null!;
    [Inject] private IMediator Mediator { get; set; } = null!;
    [Inject] ICurrentInstrumentService CurrentInstrumentService { get; set; } = null!;
    [Inject] private IPinLockoutService PinLockoutService { get; set; } = null!;

    [SupplyParameterFromForm(Name = "Input")]
    private InputModel? PostedInput { get; set; }
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromForm]
    private string? Action { get; set; }

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private bool LoginFailed()
    {
        return _errorMessage != null;
    }

    private bool IsPinMaxLength => !string.IsNullOrEmpty(Input.Pin) && Input.Pin.Length >= 8;
    private bool IsPinEmpty => string.IsNullOrEmpty(Input.Pin);
    private bool IsPinInvalid => string.IsNullOrEmpty(Input.Pin) || Input.Pin.Length < 4;

    protected override async Task OnInitializedAsync()
    {
        // Redirect to login if PIN login is not available
        if (!PinLockoutService.IsPinModeEnabled())
        {
            RedirectManager.RedirectTo(AccountRoutes.Login);
            return;
        }

        if (await PinLockoutService.IsPinLoginSuspended())
        {
            RedirectManager.RedirectTo(AccountRoutes.Login);
            return;
        }

        await RedirectManager.RedirectToRegisterIfNotInitialized(ReturnUrl);
    }

    protected override void OnParametersSet()
    {
        if (PostedInput is not null)
        {
            Input = PostedInput;

            // Handle number pad actions (number buttons and backspace)
            if (!string.IsNullOrEmpty(Action) && Action != "enter")
            {
                _errorMessage = null;
                HandleAction(Action);
            }
        }
    }

    protected override void OnInitialized()
    {
        Input.Pin = "";
    }

    public async Task LoginUser()
    {
        // Only proceed with login if Enter button was pressed
        if (Action is null or not "enter")
        {
            return;
        }

        if (SignInManager is not CustomSignInManager customSignInManager)
        {
            throw new InvalidOperationException("CustomSignInManager is required for PIN authentication.");
        }

        var (result, user) = await customSignInManager.PinSignInWithUserAsync(Input.Pin);

        if (result.Succeeded && user != null)
        {
            user.LastLoginDateTimeOffset = DateTime.Now;
            var updateResult = await SignInManager.UserManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new ValidationException("Update failed: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }

            Logger.LogInformation("User logged in with PIN. userid: {UserId}", user.Id);
            var instrumentNumber = CurrentInstrumentService.InstrumentNumber;
            await Mediator.Publish(new UserLoggedInEvent(user.Id, instrumentNumber));
            RedirectManager.RedirectTo(ReturnUrl);
            return;
        }

        if (result.IsLockedOut || result.IsNotAllowed)
        {
            RedirectManager.RedirectTo(AccountRoutes.Login);
            return;
        }

        var lockoutTriggered = await customSignInManager.HandlePinLoginFailureAsync();
        if (lockoutTriggered)
        {
            RedirectManager.RedirectTo(AccountRoutes.Login);
            return;
        }

        _errorMessage = Translations.LoginWithPin_LoginFailed;
    }

    private void HandleAction(string action)
    {
        if (action == "backspace")
        {
            if (!string.IsNullOrEmpty(Input.Pin) && Input.Pin.Length > 0)
            {
                Input.Pin = Input.Pin[..^1];

                if (string.IsNullOrEmpty(Input.Pin))
                {
                    _errorMessage = null;
                }
            }
        }
        else if (int.TryParse(action, out var number))
        {
            if (string.IsNullOrEmpty(Input.Pin))
            {
                _errorMessage = null;
                Input.Pin = number.ToString(CultureInfo.InvariantCulture);
            }
            else if (Input.Pin.Length < 8)
            {
                Input.Pin += number.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    private sealed class InputModel
    {
        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "SetPin_InvalidPin", ErrorMessageResourceType = typeof(Translations))]
        [DataType(DataType.Password)]
        [RegularExpression(@"^\d+$", ErrorMessage = "PIN must contain only digits")]
        public string Pin { get; set; } = "";
    }
}

