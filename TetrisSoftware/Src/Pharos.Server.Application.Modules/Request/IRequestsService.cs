namespace Pharos.Server.Application.Request;

public interface IRequestsService
{
    // Temp: before Request modularization...
    Task ImportRequestsFile(byte[] bytes, string file);
}
