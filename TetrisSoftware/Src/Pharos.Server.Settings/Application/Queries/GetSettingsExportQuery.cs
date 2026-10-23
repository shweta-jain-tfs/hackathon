using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Application.Identity;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Queries;

public record GetSettingsExportQuery : IRequest<string>;

internal sealed class GetSettingsExportQueryHandler(
    ISettingsDbContext context,
    IPharosVersionService pharosVersion,
    ILogger<GetSettingsExportQueryHandler> logger,
    ISender sender,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetSettingsExportQuery, string>
{
    public async Task<string> Handle(GetSettingsExportQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Export settings requested. Time={Time}", DateTime.UtcNow);

        await CreateAuditTrailRecord(cancellationToken);

        // NOTE: Do we want to prepend header information?
        var sb = new StringBuilder();
        sb.AppendLine($"Pharos ({pharosVersion.Version}) settings")
            .AppendLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}")
            .AppendLine(PageHelper.SettingsAsteriskLineAfterHeader);

        var defaultSettings = BaseDefaultSettingsProvider.GetDefaultSettingsAndPageStructure();

        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();
        var dbSettings = SettingValueStorage.ToSettingsDictionary(context.Settings, defaultOptions);

        var mergedSettings = SettingsExporter.MergeSettingsFromDBWithDefaultSettings(defaultSettings, dbSettings);

        // Identify sensitive keys so their values are not exported
        var sensitiveKeys = defaultOptions
            .Where(kv => kv.Value.IsEncrypted)
            .Select(kv => kv.Key)
            .ToHashSet(StringComparer.Ordinal);

        var exclusions = BaseDefaultSettingsProvider.GetImportExportExclusions();
        foreach (var (key, value) in mergedSettings
            .Where(kvp => !exclusions.IsExcluded(kvp.Key)))
        {
            var exportValue = sensitiveKeys.Contains(key) ? string.Empty : value;
            sb.AppendLine($"{key},{exportValue}");
        }

        // TODO: Create and append digital signature

        return await Task.FromResult(sb.ToString());
    }


    private async Task CreateAuditTrailRecord(CancellationToken cancellationToken)
    {
        if (currentUserService.UserId == null)
        {
            return;
        }
        var user = await sender.Send(new GetApplicationUserByIdQuery((Guid)currentUserService.UserId!), cancellationToken);
        await sender.Send(SettingsEvents.SettingsExportedEvent.Create(user?.UserName ?? string.Empty).ToCommand(), cancellationToken);
    }
}
