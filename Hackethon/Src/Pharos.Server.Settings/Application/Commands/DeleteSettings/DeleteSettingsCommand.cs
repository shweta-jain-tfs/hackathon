using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;
using Pharos.Server.Settings.Application.Interfaces;
using Pharos.Server.Settings.Application.Services;
using Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

namespace Pharos.Server.Settings.Application.Commands.DeleteSettings;

internal record DeleteSettingsCommand(string[] Keys) : IRequest;

internal sealed class DeleteSettingsCommandHandler(ISettingsDbContext dbContext, ILogger<DeleteSettingsCommandHandler> logger, ISender sender) : IRequestHandler<DeleteSettingsCommand>
{
    public async Task Handle(DeleteSettingsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Request to delete settings: {Settings}", string.Join(";", request.Keys));
        var settings = await dbContext.Settings
            .Where(s => request.Keys.Contains(s.Key))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (settings == null || settings.Count == 0)
        {
            throw new NotFoundException($"No settings found! Keys: {string.Join(",", request.Keys)}");
        }

        var settingsToDeleteEvent = new Dictionary<string, string>();
        var defaultOptions = BaseDefaultSettingsProvider.GetDefaultOptions();
        foreach (var setting in settings)
        {
            var option = SettingValueStorage.FindOption(defaultOptions, setting.Key);
            var oldValue = SettingValueStorage.GetEffectiveValue(setting, option);
            dbContext.Settings.Remove(setting);
            settingsToDeleteEvent.Add(setting.Key, $"Old: {oldValue}, New: (Value Deleted)");
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        // Trigger audit trail event
        await sender.Send(SettingsEvents.SettingsModifiedEvent.Create(settingsToDeleteEvent).ToCommand(), cancellationToken);
        await sender.Send(new NotifySettingsChangedCommand(DeletedKeys: settingsToDeleteEvent.Keys.ToList()), cancellationToken);
    }
}
