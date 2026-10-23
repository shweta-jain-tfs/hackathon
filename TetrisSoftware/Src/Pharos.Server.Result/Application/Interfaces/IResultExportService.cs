using Pharos.Server.Result.Application.Services.ResultExportService;

namespace Pharos.Server.Result.Application.Interfaces;

internal interface IResultExportService
{
    Task<NamedStream> GenerateResultExportStream(IEnumerable<int> analyticalRunIdList, CancellationToken cancellationToken, string? cmrRunIdUsed = null);
    Task SaveResultExportStream(IEnumerable<int> analyticalRunIdList, string folder, CancellationToken cancellationToken, string? cmrRunIdUsed = null);
}
