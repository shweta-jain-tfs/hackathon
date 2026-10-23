namespace Pharos.Server.LabCommunity.Application.Interfaces;

internal interface IApiKeyService
{
    Task<string?> GetApiKey();
}
