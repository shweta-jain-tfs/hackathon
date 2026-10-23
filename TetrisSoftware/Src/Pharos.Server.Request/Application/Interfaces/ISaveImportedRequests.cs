using Pharos.Server.Request.Application.Models;

namespace Pharos.Server.Request.Application.Interfaces;

internal interface ISaveImportedRequests
{
    Task Save(ImportedRequests requests, CancellationToken cancellationToken);
}