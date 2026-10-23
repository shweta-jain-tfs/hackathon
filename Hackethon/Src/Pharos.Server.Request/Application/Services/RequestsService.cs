using Pharos.Server.Application.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Services;

internal class RequestsService(IRequestImportService requestImportService) : IRequestsService
{
    public async Task ImportRequestsFile(byte[] bytes, string file)
    {
        await requestImportService.ImportRequestsFile(bytes, file);
    }
}