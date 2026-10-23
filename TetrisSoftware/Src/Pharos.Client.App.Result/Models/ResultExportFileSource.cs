using System.Text.RegularExpressions;

namespace Pharos.Client.App.Result.Models;

internal sealed partial class ResultExportFileSource : IFileSource<GetResultExportQuery>
{
    private readonly HttpClient _httpClient;

    public ResultExportFileSource(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FileDownloadInfo> GetFileAsync(GetResultExportQuery query)
    {
        var client = new TestResultsClient(_httpClient);
        var result = await client.GetResultExportAsync(query);
        var fileName = GetFileNameFromHeaders(result.Headers);
        return new FileDownloadInfo(fileName, result.Stream);
    }

    private static string GetFileNameFromHeaders(IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        const string contentDisposition = "Content-Disposition";
        if (!headers.ContainsKey(contentDisposition))
        {
            return "";
        }

        var headerValue = headers[contentDisposition].FirstOrDefault() ?? "";
        var match = FileNameRegex().Match(headerValue);
        return match.Groups["filename"].Value.Trim('"');
    }

    [GeneratedRegex(@"; filename=(?<filename>[^;]+)")]
    private static partial Regex FileNameRegex();

}
