using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.Wrappers.Interfaces;

public interface ISessionWrapper
{
    void Log(string message);
    void Message(InstallMessage messageType, Record record);
}