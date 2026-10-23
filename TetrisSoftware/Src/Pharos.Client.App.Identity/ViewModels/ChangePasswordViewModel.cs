namespace Pharos.Client.App.Identity.ViewModels;

public class ChangePasswordViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public PasswordPolicyContext? PasswordPolicyContext { get; set; }
}
