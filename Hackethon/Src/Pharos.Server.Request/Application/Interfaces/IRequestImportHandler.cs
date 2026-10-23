namespace Pharos.Server.Request.Application.Interfaces;

internal interface IRequestImportHandler
{
    Task Handle(byte[] fileContent, string contentFileName);
}
