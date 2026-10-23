namespace Pharos.Server.Infrastructure;

/// <summary>
/// Factory interface for creating StreamReader instances.
/// Used for better testability and abstraction.
/// </summary>
public interface IStreamReaderFactory
{
    StreamReader Create(string filePath);
}
