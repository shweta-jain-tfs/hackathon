namespace Pharos.Server.Infrastructure.Compression;

public interface IZipArchiveEntry
{
    string Name { get; }
    string FullName { get; }
    IDisposable Open();
    void ExtractToFile(string destinationFile);
}