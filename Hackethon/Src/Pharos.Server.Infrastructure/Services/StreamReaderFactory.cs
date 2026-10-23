namespace Pharos.Server.Infrastructure.Services;

internal class StreamReaderFactory : IStreamReaderFactory
{
    public StreamReader Create(string filePath)
    {
        return new StreamReader(filePath);
    }
}
