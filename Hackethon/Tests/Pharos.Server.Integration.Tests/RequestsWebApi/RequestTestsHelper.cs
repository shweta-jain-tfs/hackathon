using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Domain;

namespace Pharos.Server.Integration.Tests.RequestsWebApi;

internal static class RequestTestsHelper
{
    public static async Task EnsureTubeTypeExists(IServiceProvider serviceProvider, int tubeDefinitionId)
    {
        var settingsDbContext = serviceProvider.GetRequiredService<ISettingsDbContext>();

        var tubeTypeIdExists = await settingsDbContext.Settings
            .AnyAsync(x => x.Key.Contains("Samples:TubeTypes:TubeTypeList:")
                           && x.Key.EndsWith(":TubeDefinitionId")
                           && x.Value == tubeDefinitionId.ToString(), TestContext.Current.CancellationToken);

        if (!tubeTypeIdExists)
        {
            settingsDbContext.Settings.Add(new Setting
            {
                Key = "Samples:TubeTypes:TubeTypeList:0:Name",
                Value = $"TubeType {tubeDefinitionId}"
            });

            settingsDbContext.Settings.Add(new Setting
            {
                Key = "Samples:TubeTypes:TubeTypeList:0:TubeDefinitionId",
                Value = tubeDefinitionId.ToString()
            });
        }

        var normalTubeDefinition = await settingsDbContext.Settings
            .FirstOrDefaultAsync(x => x.Key == "Samples:TubeTypes:NormalTubeDefinitionId", TestContext.Current.CancellationToken);

        if (normalTubeDefinition == null)
        {
            settingsDbContext.Settings.Add(new Setting
            {
                Key = "Samples:TubeTypes:NormalTubeDefinitionId",
                Value = tubeDefinitionId.ToString()
            });
        }

        if (await settingsDbContext.SaveChangesAsync(TestContext.Current.CancellationToken) > 0)
        {
            await Task.Delay(50);
        }
    }

    public static async Task<int> GetValidSampleTubeType(IServiceProvider serviceProvider)
    {
        var tubeSettings = serviceProvider.GetRequiredService<IOptionsSnapshot<TubeTypesSettings>>().Value;

        var configuredTubeType = tubeSettings.TubeTypeList.FirstOrDefault();
        if (configuredTubeType != null)
        {
            return configuredTubeType.TubeDefinitionId;
        }

        if (int.TryParse(tubeSettings.NormalTubeDefinitionId, out var normalTubeTypeId))
        {
            await EnsureTubeTypeExists(serviceProvider, normalTubeTypeId);
            return normalTubeTypeId;
        }

        const int fallbackTubeTypeId = 1;
        await EnsureTubeTypeExists(serviceProvider, fallbackTubeTypeId);
        return fallbackTubeTypeId;
    }
}
