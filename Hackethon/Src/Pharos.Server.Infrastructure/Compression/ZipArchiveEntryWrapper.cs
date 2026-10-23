using System.IO.Compression;

namespace Pharos.Server.Infrastructure.Compression;

internal sealed class ZipArchiveEntryWrapper : IZipArchiveEntry
{
    private readonly ZipArchiveEntry _inner;

    public ZipArchiveEntryWrapper(ZipArchiveEntry inner)
    {
        _inner = inner;
    }

    public string Name => _inner.Name;
    public string FullName => _inner.FullName;

    public IDisposable Open()
    {
        return _inner.Open();
    }

    public void ExtractToFile(string destinationFile)
    {
        _inner.ExtractToFile(destinationFile);
    }
}