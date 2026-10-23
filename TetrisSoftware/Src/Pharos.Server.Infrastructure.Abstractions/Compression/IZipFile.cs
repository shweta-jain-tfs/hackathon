namespace Pharos.Server.Infrastructure.Compression;

public interface IZipFile
{
    IZipArchive OpenRead(string path);

    IZipWriter CreateWriter(Stream destination);
}