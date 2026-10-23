using CustomAction.Wrappers.Interfaces;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomAction.Wrappers;

public class SessionWrapper(Session session) : ISessionWrapper
{
    public void Log(string message)
    {
        session.Log(message);
    }

    public void Message(InstallMessage messageType, Record record)
    {
        session.Message(messageType, record);
    }
}