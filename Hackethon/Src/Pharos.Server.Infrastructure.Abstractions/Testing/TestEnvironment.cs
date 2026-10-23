namespace Pharos.Server.Infrastructure.Testing;

public static class TestEnvironment
{
    public static bool IsXUnitDetected()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.FullName?.Contains("xunit", StringComparison.InvariantCultureIgnoreCase) ?? false);
    }
}