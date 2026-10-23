namespace Pharos.Server.SystemUpdate.Application.Interfaces;

internal interface IUpdatePackageAccessor
{
    /// <summary>
    /// Enumerates the contents of the update package located in the specified directory.
    /// </summary>
    List<string> EnumerateContents(string packageDir, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the update package to a temporary directory and returns the path.
    /// </summary>
    string StageToDirectory(string packageDir, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a directory and returns update candidates.
    /// </summary>
    List<string> FindUpdateCandidates(string directory, CancellationToken cancellationToken = default);
}