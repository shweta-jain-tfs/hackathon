using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.LabCommunity;
using Pharos.Server.LabCommunity.Application.Interfaces;

namespace Pharos.Server.LabCommunity.Application.Services;

internal class ApiKeyService(IServiceScopeFactory scopeFactory) : IApiKeyService
{
    public async Task<string?> GetApiKey()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ILabCommunityDbContext>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<LabCommunitySettings>>();

        var enrollment = await dbContext.GetActiveEnrollmentAsync(settings.Value.LaboratoryCode);

        return enrollment?.ApiKey;
    }
}
