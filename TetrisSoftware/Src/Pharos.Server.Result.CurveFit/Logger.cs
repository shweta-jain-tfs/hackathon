namespace Pharos.Server.Result.CurveFit;

public delegate void LogInformation(string? message, params object?[] args);

public static class Logger
{
    public static LogInformation? LogText { private get; set; }

    public static void Log(string text, params object?[] args)
    {
        LogText?.Invoke(text, args);
    }
}
