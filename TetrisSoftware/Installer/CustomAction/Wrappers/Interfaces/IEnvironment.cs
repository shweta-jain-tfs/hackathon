namespace CustomAction.Wrappers.Interfaces;

public interface IEnvironment
{
    string GetMachineName();
    string GetCommonAppDataPath();
}