using System.IO.Compression;

namespace Pharos.Server.Infrastructure.Compression;

internal sealed class ZipArchiveWrapper : IZipArchive
{
    private readonly ZipArchive _inner;

    public ZipArchiveWrapper(ZipArchive inner)
    {
        _inner = inner;
    }

    public IReadOnlyCollection<IZipArchiveEntry> Entries =>
        field ??= _inner.Entries.Select(IZipArchiveEntry (e) => new ZipArchiveEntryWrapper(e)).ToList();

    public void Dispose()
    {
        _inner.Dispose();
    }
}