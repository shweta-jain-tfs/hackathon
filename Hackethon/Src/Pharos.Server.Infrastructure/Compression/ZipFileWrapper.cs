using System.IO.Compression;

namespace Pharos.Server.Infrastructure.Compression;

internal sealed class ZipFileWrapper : IZipFile
{
    public IZipArchive OpenRead(string path)
    {
        return new ZipArchiveWrapper(ZipFile.OpenRead(path));
    }

    public IZipWriter CreateWriter(Stream destination)
    {
        return new ZipWriter(destination);
    }
}