namespace CustomAction.Wrappers.Interfaces;

public interface IProcessRunner
{
    int Run(string fileName, string arguments, string workingDirectory);
}