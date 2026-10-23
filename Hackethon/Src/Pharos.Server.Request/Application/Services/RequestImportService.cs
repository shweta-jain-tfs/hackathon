using MediatR;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Services;

internal sealed class RequestImportService : IRequestImportService
{
    private readonly IRequestImportHandler _requestImportHandler;
    private readonly ISender _sender;

    public RequestImportService(
        IRequestImportHandler requestImportHandler,
        ISender sender)
    {
        _requestImportHandler = requestImportHandler;
        _sender = sender;
    }

    public async Task ImportRequestsFile(string file)
    {
        if (File.Exists(file))
        {
            await using var stream = File.OpenRead(file);
            var bytes = await GetBytes(stream);
            await ImportRequestsFile(bytes, file);
        }
    }

    public async Task ImportRequestsFile(byte[] fileContent, string contentFileName)
    {
        try
        {
            await _requestImportHandler.Handle(fileContent, contentFileName);
        }
        catch (Exception exception)
        {
            await _sender.Send(new CreatePharosErrorCommand(PharosErrorCodes.RequestImportFailed, ErrorClassification.Error, Parameters: [contentFileName, exception.Message]));
            throw;
        }
    }

    /// <summary>
    /// Return bytes in a stream
    /// </summary>
    /// <param name="fileStream">File stream</param>
    /// <returns>Array of bytes</returns>
    private static async Task<byte[]> GetBytes(FileStream fileStream)
    {
        await using var memStream = new MemoryStream();
        await fileStream.CopyToAsync(memStream);
        return memStream.ToArray();
    }

}
