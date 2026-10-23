namespace Pharos.Server.Infrastructure.Compression;

public interface IZipArchive : IDisposable
{
    IReadOnlyCollection<IZipArchiveEntry> Entries { get; }
}