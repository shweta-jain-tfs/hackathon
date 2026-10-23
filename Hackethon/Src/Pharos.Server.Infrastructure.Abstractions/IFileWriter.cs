namespace Pharos.Server.Infrastructure;

public interface IFileWriter
{
    Task WriteAsync(string fileName, string folder, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    Task WriteToSmbShareAsync(string username, string password, string host, string path, string fileName,
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

    Task WriteToScpAsync(string username, string password, string host, string path, string fileName,
        ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

}