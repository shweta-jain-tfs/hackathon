using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Handlers;

internal class RequestImportHandler : IRequestImportHandler
{
    private readonly IRequestImportFileContentParser _parser;
    private readonly ISaveImportedRequests _saveImportedRequests;

    public RequestImportHandler(IRequestImportFileContentParser parser, ISaveImportedRequests saveImportedRequests)
    {
        _parser = parser;
        _saveImportedRequests = saveImportedRequests;
    }

    public async Task Handle(byte[] fileContent, string contentFileName)
    {
        var fileContentAsString = await GetFileContent(fileContent);
        var requests = _parser.Parse(fileContentAsString, contentFileName);

        await _saveImportedRequests.Save(requests, CancellationToken.None);
    }

    private static async Task<string> GetFileContent(byte[] fileContent)
    {
        await using var stream = new MemoryStream(fileContent);
        var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        reader.Close();
        reader.Dispose();

        return content;
    }
}
