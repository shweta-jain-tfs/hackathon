using System.IO.Compression;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Services;

internal class MethodUpdatePackage(string filePath, string? originalFileName) : IMethodUpdatePackage
{
    private ZipArchive? _zip;

    private ZipArchive Zip
    {
        get
        {
            _zip ??= ZipFile.OpenRead(filePath);
            return _zip;
        }
    }

    public void Dispose()
    {
        _zip?.Dispose();
    }

    public string Name => originalFileName ?? Path.GetFileName(filePath);

    public async Task<string> GetDescription()
    {
        var entry = Zip.Entries.FirstOrDefault(x => x.Name.Equals("description.txt", StringComparison.OrdinalIgnoreCase));
        if (entry == null)
        {
            return "";
        }

        await using var stream = entry.Open();
        using var reader = new StreamReader(stream, true);
        return await reader.ReadToEndAsync();
    }

    public Task<bool> ValidateChecksums()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<string> GetFileNames()
    {
        return Zip.Entries.Select(x => x.Name).ToList();
    }

    public async Task<byte[]> GetFileContent(string fileName)
    {
        var entry = Zip.Entries.First(x => x.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        await using var stream = entry.Open();
        var result = new byte[entry.Length];
        var memoryStream = new MemoryStream(result);
        await stream.CopyToAsync(memoryStream);
        return result;
    }
}
