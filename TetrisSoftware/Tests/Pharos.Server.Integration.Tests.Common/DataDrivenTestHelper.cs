namespace Pharos.Server.Integration.Tests.Common;

public static class DataDrivenTestHelper
{
    /// <summary>
    /// Calculates the base path for the data files used in a test class.
    /// This method assumes that the data files are located next to the class and copied
    /// to the output directory during the build.
    /// </summary>
    public static string GetTestDataFilesPath(Type testClass)
    {
        var assembly = testClass.Assembly;
        var assemblyName = assembly.GetName().Name!;
        const string testsSuffix = ".Tests";
        var rootNamespace = assemblyName.EndsWith(testsSuffix, StringComparison.OrdinalIgnoreCase) ? assemblyName[..^testsSuffix.Length] : assemblyName;
        var testNamespace = testClass.Namespace!;
        if (!testNamespace.StartsWith(rootNamespace))
        {
            throw new InvalidOperationException($"The test class namespace {testNamespace} does not match the default assembly namespace {rootNamespace}");
        }
        var assemblyFolder = Path.GetDirectoryName(assembly.Location)!;
        var subFolder = testNamespace[(rootNamespace.Length + 1)..].Replace('.', '\\');
        var result = Path.Combine(assemblyFolder, subFolder);
        return result;
    }
}
