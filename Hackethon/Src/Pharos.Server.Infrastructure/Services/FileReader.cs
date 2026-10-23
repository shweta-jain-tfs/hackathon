namespace Pharos.Server.Infrastructure.Services;

internal class FileReader : IFileReader
{
    public async Task<byte[]> ReadAllBytesAsync(string fileName, string folder, CancellationToken cancellationToken)
    {
        var expandedFolder = Environment.ExpandEnvironmentVariables(folder);
        var fullPath = Path.Combine(expandedFolder, fileName);

        if (File.Exists(fullPath))
        {
            return await File.ReadAllBytesAsync(fullPath, cancellationToken);
        }

        return Array.Empty<byte>();
    }
}