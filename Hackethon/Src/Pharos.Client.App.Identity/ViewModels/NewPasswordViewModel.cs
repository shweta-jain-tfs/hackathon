namespace Pharos.Client.App.Identity.ViewModels;

public class NewPasswordViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public PasswordPolicyContext? PasswordPolicyContext { get; set; }
}
