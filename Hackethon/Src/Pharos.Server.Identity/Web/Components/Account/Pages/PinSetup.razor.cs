using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using Pharos.Server.Application;
using Pharos.Server.Application.Settings.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Web.Components.Account.Pages;

public partial class PinSetup : ComponentBase
{
    #region Parameters

    private string Pin { get; set; } = string.Empty;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    #endregion

    #region Services

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IOptionsSnapshot<SecuritySettings> SecuritySettings { get; set; } = null!;
    [Inject] ICurrentUserService CurrentUserService { get; set; } = null!;
    [Inject] private IApplicationUserManager UserManager { get; set; } = null!;
    [Inject] private ILogger<PinSetup> Logger { get; set; } = null!;

    #endregion

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(ReturnUrl))
        {
            ReturnUrl = "/";
        }

        // Prevent open redirects.
        if (!Uri.IsWellFormedUriString(ReturnUrl, UriKind.Relative))
        {
            ReturnUrl = NavigationManager.ToBaseRelativePath(ReturnUrl);
        }

        if (!SecuritySettings.Value.EnablePinLogin)
        {
            // accidentally navigated to this page, redirect immediately to the return URL or home page
            NavigationManager.NavigateTo(ReturnUrl, forceLoad: true);
            return;
        }

        try
        {
            var userId = CurrentUserService.UserId ?? Guid.Empty;
            var user = await UserManager.FindByGuidAsync(userId);
            if (user?.NeedsNewPin == true)
            {
                // Handle PIN setup logic here
                Logger.LogInformation("Generating a PIN-code for user: {UserId}", user.Id);
                var generatedPin = await GenerateUniquePinAsync(user);
                if (generatedPin is null)
                {
                    Logger.LogError("Failed to generate a unique PIN for user: {UserId}", user.Id);
                }
                else
                {
                    Pin = generatedPin;
                    return;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occurred during PIN setup.");
        }

        // In case of any error, or repeated navigation to this page, redirect to the return URL or home page.
        NavigationManager.NavigateTo(ReturnUrl, forceLoad: true);
    }

    /// <summary>
    /// Generates a unique 4-digit PIN for a user. Attempts up to 100 times to find a unique PIN.
    /// </summary>
    /// <param name="user">The user to generate a PIN for.</param>
    /// <returns>The generated PIN if successful, null if unable to generate a unique PIN after max attempts.</returns>
    private async Task<string?> GenerateUniquePinAsync(ApplicationUser user)
    {
        const int maxAttempts = 100;
        var attempts = 0;

        while (attempts < maxAttempts)
        {
            // Generate a random 4-digit PIN (0000-9999) using cryptographically secure random number generator
            var pin = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 10000).ToString("D4", CultureInfo.InvariantCulture);

            // Try to set the PIN
            var testResult = await UserManager.SetPinAsync(user, pin);
            if (testResult.Succeeded)
            {
                return pin;
            }

            attempts++;
        }
        return null;
    }
}
