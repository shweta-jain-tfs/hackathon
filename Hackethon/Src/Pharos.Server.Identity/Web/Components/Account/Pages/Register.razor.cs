using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Infrastructure;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public partial class Register
{
    private IEnumerable<IdentityError>? _identityErrors;

    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private ILogger<Register> Logger { get; set; } = default!;
    [Inject] private IIdentityRedirectManager RedirectManager { get; set; } = default!;
    [Inject] private IApplicationUserManager UserManager { get; set; } = default!;
    [Inject] private IUserStore<ApplicationUser> UserStore { get; set; } = default!;
    [Inject] private IDateTime DateTime { get; set; } = default!;

    [SupplyParameterFromForm(Name = "Input")]
    private InputModel? PostedInput { get; set; }
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private string? Message => _identityErrors is null ? null : string.Join("\n", _identityErrors.Select(error => error.Description));

    private bool HasPasswordComplexityError => !string.IsNullOrEmpty(PasswordError);
    private string? PasswordError { get; set; }

    public async Task RegisterUser()
    {
        if (!UserManager.IsPasswordValid(Input.Password, Input.UserName))
        {
            PasswordError = Translations.ChangePassword_PasswordComplexityError;
            return;
        }

        PasswordError = null;

        var user = new ApplicationUser
        {
            LastLoginDateTimeOffset = DateTime.Now,
            TemporaryPassword = false,
        };

        await UserStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
        var result = await UserManager.CreateAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            _identityErrors = result.Errors;
            return;
        }

        await UserManager.AddToRoleAsync(user, BuiltInRoles.ServiceEngineer);

        Logger.LogInformation("Service user is created.");

        await SignInManager.SignInAsync(user, isPersistent: false);
        Logger.LogInformation("The service user is automatically signed in.");

        RedirectManager.RedirectTo(ReturnUrl);
    }

    protected override async Task OnInitializedAsync()
    {
        // we only allow to register the first user (temporarily)
        if (await UserManager.AreUsersInitialized())
        {
            RedirectManager.RedirectTo(AccountRoutes.Login);
        }
    }

    protected override void OnParametersSet()
    {
        // If the form post supplied Input, use it as the current model
        if (PostedInput is not null)
        {
            Input = PostedInput;
            _identityErrors = null;
            PasswordError = null;
        }
    }

    private sealed class InputModel
    {
        private const int _passwordMax = 1000;

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "UserNameRequired", ErrorMessageResourceType = typeof(Translations))]
        public string UserName { get; set; } = "";

        [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "PasswordRequired", ErrorMessageResourceType = typeof(Translations))]
        [StringLength(_passwordMax, MinimumLength = Contracts.Security.Password.DefaultMinimumLength, ErrorMessageResourceName = "Register_PasswordLengthIncorrect", ErrorMessageResourceType = typeof(Translations))]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessageResourceName = "Register_PasswordsDoNotMatch", ErrorMessageResourceType = typeof(Translations))]
        public string ConfirmPassword { get; set; } = "";
    }
}

