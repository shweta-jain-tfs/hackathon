using MediatR;
using Pharos.Server.Application.LogDump;
using Pharos.Server.Infrastructure;
using Pharos.Server.LogDump.Application.Interfaces;

namespace Pharos.Server.LogDump.Application.Queries.GetLogDumpCollectionResult;

internal sealed record GetLogDumpCollectionResultQuery : IRequest<LogDumpCollectionResultDto>;

internal sealed class GetLogDumpCollectionResultQueryHandler(
    ILogDumpResultStore logDumpResultStore,
    ITempFileStorage tempFileStorage)
    : IRequestHandler<GetLogDumpCollectionResultQuery, LogDumpCollectionResultDto>
{
    public async Task<LogDumpCollectionResultDto> Handle(GetLogDumpCollectionResultQuery request, CancellationToken cancellationToken)
    {
        var result = logDumpResultStore.Result;
        if (result == null || result.Source != LogDumpRequestSource.Pharos)
        {
            return new LogDumpCollectionResultDto();
        }

        var filePath = tempFileStorage.GetFilePath(result.FileId);
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return new LogDumpCollectionResultDto();
        }

        return new LogDumpCollectionResultDto
        {
            FileId = result.FileId,
            FileName = result.FileName,
            FileCreatedDateTime = result.FileCreatedDateTime
        };
    }
}