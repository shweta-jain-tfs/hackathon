using System.Globalization;
using Pharos.Client.App.Identity.Models;
using Pharos.Client.Common.App.Interfaces;
using Pharos.Client.Common.App.Services;

namespace Pharos.Client.App.Identity.ViewModels;

/// <summary>
/// ViewModel for the user's own account page.
/// Displays read-only account information for the currently logged-in user.
/// </summary>
public class UserAccountPageViewModel : ViewModelBase
{
    private readonly IApplicationUserDataService _dataService;
    private readonly IStringLocalizer<Translations> _loc;
    private readonly UiSettingsService _uiSettingsService;
    private readonly IDateTime _dateTime;

    public UserAccountPageViewModel(IApplicationUserDataService dataService, IStringLocalizer<Translations> loc, UiSettingsService uiSettingsService, IDateTime dateTime)
    {
        _dataService = dataService;
        _loc = loc;
        _uiSettingsService = uiSettingsService;
        _dateTime = dateTime;
    }

    public ApplicationUser? CurrentUser
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsLoading
    {
        get;
        set => SetField(ref field, value);
    } = true;

    public string? ErrorMessage
    {
        get;
        set => SetField(ref field, value);
    }

    public bool IsPinLoginEnabled { get; private set; }
    public bool IsPasswordExpirationEnabled { get; private set; }

    public IEnumerable<(string PermissionKey, string translatedName)> Permissions
    {
        get
        {
            if (CurrentUser?.Role?.Permissions == null)
            {
                return Enumerable.Empty<(string, string)>();
            }

            return CurrentUser.Role.Permissions
                .Select(permission => (
                    PermissionKey: permission,
                    translatedName: _loc[$"Role_{permission}"].ToString()
                ))
                .OrderBy(x => x.translatedName);
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            // Fetch current user details
            var currentUser = await _dataService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                ErrorMessage = _loc["UserAccountPage_Error_UserNotFound"];
                CurrentUser = null;
            }
            else
            {
                CurrentUser = currentUser;
            }

            // Fetch PIN login settings
            var settings = await _uiSettingsService.GetSettingsAsync();
            IsPinLoginEnabled = settings.SecuritySettings.EnablePinLogin;
            IsPasswordExpirationEnabled = settings.SecuritySettings.PasswordExpirationEnabled;
        }
        catch (Exception)
        {
            ErrorMessage = _loc["UserAccountPage_Error_LoadingFailed"];
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ChangeCurrentUserPasswordAsync(string currentPassword, string newPassword)
    {
        await _dataService.ChangeCurrentUserPasswordAsync(currentPassword, newPassword);
    }

    public string GetPasswordExpirationDisplay()
    {
        if (CurrentUser?.PasswordExpirationDateTimeOffset == null)
        {
            return _loc["UserAccountPage_PasswordExpiration_NotSet"];
        }

        var expirationDate = CurrentUser.PasswordExpirationDateTimeOffset.Value;
        var today = _dateTime.Now;

        if (expirationDate < today)
        {
            return _loc["UserAccountPage_PasswordExpiration_Expired"];
        }

        var daysUntilExpiration = (expirationDate.Date - today.Date).Days;
        if (daysUntilExpiration == 0)
        {
            return _loc["UserAccountPage_PasswordExpiration_Today"];
        }

        return expirationDate.ToString("d", CultureInfo.CurrentCulture);
    }
}
