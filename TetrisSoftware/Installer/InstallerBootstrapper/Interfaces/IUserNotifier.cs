namespace InstallerBootstrapper;

public interface IUserNotifier
{
    void ShowMessage(string message, string caption);
    void WriteLine(string line);
}