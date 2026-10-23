namespace Pharos.Server.Parameters.Application.Interfaces;

/// <summary>
/// Represents an abstraction of a method update package (.zip file).
/// </summary>
internal interface IMethodUpdatePackage : IDisposable
{
    /// <summary>
    /// Gets the package file name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the method update description.
    /// </summary>
    /// <returns>
    /// The contents of the description.txt file inside the package or an empty string if the file is not present.
    /// </returns>
    Task<string> GetDescription();

    /// <summary>
    /// Validates checksums of the files within the package.
    /// </summary>
    /// <returns>True if all files have valid checksums, otherwise, false.</returns>
    Task<bool> ValidateChecksums();

    /// <summary>
    /// Gets names of the files contained in the package.
    /// </summary>
    /// <returns>The list of file names.</returns>
    IReadOnlyList<string> GetFileNames();

    /// <summary>
    /// Extracts content of a single file in the package.
    /// </summary>
    /// <param name="fileName">A file name within the package to extract.</param>
    /// <returns>A byte array with the file content.</returns>
    Task<byte[]> GetFileContent(string fileName);
}
