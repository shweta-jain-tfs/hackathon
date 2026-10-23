namespace Pharos.Server.Parameters.Application.Interfaces;

internal interface IMethodUpdatePackageFactory
{
    /// <summary>
    /// Creates a <see cref="IMethodUpdatePackage"/> instance pointing to the specified file name.
    /// </summary>
    /// <param name="filePath">The full path to the method update package file (.zip).</param>
    /// <param name="originalFileName">The original package file name (if the file is temporary).</param>
    /// <returns>The <see cref="IMethodUpdatePackage"/> instance.</returns>
    IMethodUpdatePackage Create(string filePath, string? originalFileName);
}
