using CustomAction.Wrappers.Interfaces;

namespace CustomAction.Wrappers;

public class LocalEnvironment : IEnvironment
{
    public string GetMachineName()
    {
        return System.Environment.MachineName;
    }

    public string GetCommonAppDataPath()
    {
        return System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
    }
}