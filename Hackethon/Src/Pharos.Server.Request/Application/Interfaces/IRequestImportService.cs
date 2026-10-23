namespace Pharos.Server.Request.Application.Interfaces;

internal interface IRequestImportService
{
    Task ImportRequestsFile(string file);
    Task ImportRequestsFile(byte[] bytes, string file);
}
