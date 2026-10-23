namespace Pharos.Client.App.Identity.ViewModels;

public class NewUserViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public PasswordPolicyContext? PasswordPolicyContext { get; set; }
    public HashSet<string> ExistingUserNames { get; set; } = new();
}
