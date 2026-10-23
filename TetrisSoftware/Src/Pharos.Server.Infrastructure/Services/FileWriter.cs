using System.Net;
using Microsoft.Extensions.Logging;

namespace Pharos.Server.Infrastructure.Services;

internal class FileWriter : IFileWriter
{
    private readonly ILogger<FileWriter> _logger;

    public FileWriter(ILogger<FileWriter> logger)
    {
        _logger = logger;
    }

    public async Task WriteAsync(string fileName, string folder, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var expandedFolder = Environment.ExpandEnvironmentVariables(folder);
        if (!Directory.Exists(expandedFolder) && !string.IsNullOrWhiteSpace(expandedFolder))
        {
            Directory.CreateDirectory(expandedFolder);
        }
        var fullPath = Path.Combine(expandedFolder, fileName);
        await using var fileStream = File.Create(fullPath);
        await fileStream.WriteAsync(data, cancellationToken);
    }

    /// <summary>
    /// Try...catch when using this
    /// </summary>
    public async Task WriteToScpAsync(string username, string password, string host, string path, string fileName,
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    /// <summary>
    /// Try...catch when using this
    /// host may be empty if path contains the server name
    /// if path does not contain the server name, it must be a UNC path, as in "\\server\share\folder\"
    /// </summary>
    public async Task WriteToSmbShareAsync(string username, string password, string host, string path, string fileName,
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var uncPath = SmbConnection.ToUncPath(host, path);
        if (!string.IsNullOrEmpty(uncPath))
        {
            using (new SmbConnection(SmbConnection.GetShareRoot(uncPath), new NetworkCredential(username, password), _logger))
            {
                var filePath = Path.Combine(uncPath, fileName);
                await using var fileStream = File.Create(filePath);
                await fileStream.WriteAsync(data, cancellationToken);
            }
        }
        else
        {
            throw new ArgumentException("The path is not a valid UNC path.", nameof(path));
        }
    }
}
