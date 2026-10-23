namespace Pharos.Server.Infrastructure.Compression;

public interface IZipWriter : IAsyncDisposable
{
    Task AddAsync(string entryName, Stream source, CancellationToken cancellationToken);
}