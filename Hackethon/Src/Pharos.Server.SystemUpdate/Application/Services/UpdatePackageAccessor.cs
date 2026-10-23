using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Pharos.Server.Infrastructure.Compression;
using Pharos.Server.SystemUpdate.Application.Interfaces;

namespace Pharos.Server.SystemUpdate.Application.Services;

internal class UpdatePackageAccessor : IUpdatePackageAccessor
{
    private readonly IZipFile _zipFile;
    private readonly IFileSystem _fs;
    private readonly ILogger<UpdatePackageAccessor> _logger;
    private static readonly HashSet<string> _supportedExt = new(StringComparer.OrdinalIgnoreCase) { ".MSI", ".MSU", ".EXE" };
    private readonly string _tempExtractRoot;

    public UpdatePackageAccessor(IZipFile zipFile, IFileSystem fs, ILogger<UpdatePackageAccessor> logger)
    {
        _zipFile = zipFile;
        _fs = fs;
        _logger = logger;

        _tempExtractRoot = Path.Combine(_fs.Path.GetTempPath(), "Pharos", "SystemUpdate");
    }

    public List<string> EnumerateContents(string packageDir, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var zipPath = GetSinglePackagePathOrThrow(packageDir);
        using var archive = _zipFile.OpenRead(zipPath);

        var updates = archive.Entries
            .Where(IsValidEntry)
            .Select(entry => entry.Name)
            .ToList();

        return updates;
    }

    public string StageToDirectory(string packageDir, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_fs.Directory.Exists(_tempExtractRoot))
        {
            DeletePrevTempFiles(_tempExtractRoot);
        }
        else
        {
            _fs.Directory.CreateDirectory(_tempExtractRoot);
        }

        var zipPath = GetSinglePackagePathOrThrow(packageDir);

        using var archive = _zipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries.Where(IsValidEntry))
        {
            var destination = Path.Combine(_tempExtractRoot, entry.Name);
            CopyEntryToDestination(entry, destination);
        }

        return _tempExtractRoot;
    }

    public List<string> FindUpdateCandidates(string directory, CancellationToken cancellationToken = default)
    {
        var candidates = _fs.Directory
            .EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => _supportedExt.Contains(Path.GetExtension(f).ToUpperInvariant()))
            .ToList();

        return candidates;
    }

    private void DeletePrevTempFiles(string tempExtractRoot)
    {
        foreach (var existing in _fs.Directory.EnumerateFiles(tempExtractRoot, "*", SearchOption.AllDirectories))
        {
            try
            {
                _fs.File.Delete(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete temp file {File}.", existing);
            }
        }

        _logger.LogDebug("Cleaned existing temp extraction folder {TempExtractRoot}.", tempExtractRoot);
    }

    private string GetSinglePackagePathOrThrow(string packageDir)
    {
        var zipPackages = _fs.Directory.EnumerateFiles(packageDir, "*.zip", SearchOption.AllDirectories).ToList();
        if (!ValidateUpdatePackage(zipPackages, packageDir))
        {
            throw new InvalidDataException("Update package validation failed.");
        }

        return zipPackages.First();
    }

    private bool ValidateUpdatePackage(List<string> zipPackages, string searchRoot)
    {
        if (zipPackages.Count == 0)
        {
            _logger.LogError("No update package found at {SearchRoot}.", searchRoot);

            return false;
        }

        if (zipPackages.Count > 1)
        {
            _logger.LogError("Multiple update packages found at {SearchRoot}. This is undefined.", searchRoot);

            return false;
        }

        return true;
    }

    private bool IsValidEntry(IZipArchiveEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Name))
        {
            return false;
        }

        var ext = Path.GetExtension(entry.Name).ToUpperInvariant();
        if (!_supportedExt.Contains(ext))
        {
            return false;
        }

        // Mitigate zip slip vulnerabilities.
        if (entry.FullName.Contains("..", StringComparison.Ordinal))
        {
            _logger.LogWarning("Skipping suspicious entry {Entry}.", entry.FullName);
            return false;
        }

        return true;
    }

    private void CopyEntryToDestination(IZipArchiveEntry sourceEntry, string destinationFile)
    {
        sourceEntry.ExtractToFile(destinationFile);

        _logger.LogInformation("Copied installer sourceEntry {EntryName} to {Destination}.", sourceEntry.Name, destinationFile);
    }
}
