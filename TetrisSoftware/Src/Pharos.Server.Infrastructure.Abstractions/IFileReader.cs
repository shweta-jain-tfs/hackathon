namespace Pharos.Server.Infrastructure;

public interface IFileReader
{
    Task<byte[]> ReadAllBytesAsync(string fileName, string folder, CancellationToken cancellationToken);
}