using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services.ResultExportService;

namespace Pharos.Server.Result.Application.Queries.GetResultExport;

public record GetResultExportQuery(int[] AnalyticalRunIds) : IRequest<NamedStream>;

internal sealed class GetResultExportQueryHandler(IResultExportService resultExportService, ILogger<GetResultExportQueryHandler> logger)
    : IRequestHandler<GetResultExportQuery, NamedStream>
{

    /// <summary>
    /// Create and return a Test Result stream object + suggested file name for the specified analytical runs.
    /// </summary>
    /// <param name="request">Query containing conditions for the export - analytical run IDs to export.</param>
    /// <param name="cancellationToken">Token for cancelling async operations.</param>
    /// <returns cref="NamedStream">Data stream and suggested name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If no analytical run ID is supplied.</exception>
    public async Task<NamedStream> Handle(GetResultExportQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Download result export file for analytical runs {AnalyticalRunsString}.", string.Join(", ", request.AnalyticalRunIds));

        if (request.AnalyticalRunIds.Length == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Analytical run ID missing.");
        }

        var csvFile = await resultExportService.GenerateResultExportStream(request.AnalyticalRunIds, cancellationToken);

        logger.LogInformation("Result export file {FileName} created.", csvFile.FileName);

        return csvFile;
    }

}

