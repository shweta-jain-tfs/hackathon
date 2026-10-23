using System.IO.Compression;

namespace Pharos.Server.Infrastructure.Compression;

internal sealed class ZipWriter : IZipWriter
{
    private readonly ZipArchive _archive;

    public ZipWriter(Stream destination)
    {
        _archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
    }

    public async Task AddAsync(string entryName, Stream source, CancellationToken cancellationToken)
    {
        var entry = _archive.CreateEntry(entryName, CompressionLevel.Optimal);

        await using var entryStream = entry.Open();

        await source.CopyToAsync(entryStream, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _archive.Dispose();
        return ValueTask.CompletedTask;
    }
}