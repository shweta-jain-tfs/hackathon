using MediatR;
using Pharos.Server.Application.Settings;
using Pharos.Server.Application.Settings.Lis;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Parameters.Application.Interfaces;

namespace Pharos.Server.Parameters.Application.Events;

/// <summary>
/// Handles changes to <see cref="LisSettings"/> and resets DefaultLisChannelId
/// on any method that references a channel that has been deleted.
/// </summary>
internal sealed class LisSettingsChangedEventHandler(
    IParametersDbContext dbContext, ISender sender)
    : INotificationHandler<SettingsChangedEvent<LisSettings>>
{
    public async Task Handle(SettingsChangedEvent<LisSettings> notification, CancellationToken cancellationToken)
    {
        await HandleDeletedChannels(notification, cancellationToken);
        await HandleForceUniqueLaboratoryTestNames(notification, cancellationToken);
    }

    private async Task HandleDeletedChannels(SettingsChangedEvent<LisSettings> notification, CancellationToken cancellationToken)
    {
        var newChannels = notification.NewValue.LisChannels;
        var oldChannels = notification.OldValue.LisChannels;
        if (newChannels.Count >= oldChannels.Count)
        {
            return;
        }
        var newChannelNames = newChannels.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        var deletedChannelIds = oldChannels
           .Select((channel, index) => (channel.Name, index))
           .Where(x => !newChannelNames.Contains(x.Name))
           .Select(x => x.index)
           .ToHashSet();
        if (deletedChannelIds.Count == 0)
        {
            return;
        }

        var lowestDeletedChannelId = deletedChannelIds.Min();

        var methods = await dbContext.Methods
         .Where(m => m.DefaultLisChannelId != null && m.DefaultLisChannelId >= lowestDeletedChannelId)
         .ToListAsync(cancellationToken)
         .ConfigureAwait(false);

        foreach (var method in methods)
        {
            var channelId = method.DefaultLisChannelId!.Value;
            method.DefaultLisChannelId = deletedChannelIds.Contains(channelId)
                ? null // Channel was deleted
                : channelId - deletedChannelIds.Count(d => d < channelId);  // Shift down as channel ids are index based
        }

        if (dbContext.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleForceUniqueLaboratoryTestNames(SettingsChangedEvent<LisSettings> notification, CancellationToken cancellationToken)
    {
        var wasForceUniqueEnabled = notification.OldValue.LisChannels
            .Any(x => x.ImportSettings.ForceUniqueLaboratoryTestNames);

        var isForceUniqueEnabled = notification.NewValue.LisChannels
            .Any(x => x.ImportSettings.ForceUniqueLaboratoryTestNames);

        if (wasForceUniqueEnabled || !isForceUniqueEnabled)
        {
            return;
        }

        // Fetch every lab name that appears under more than one MethodTest, together with all
        // affected MethodTest ids, so the client can highlight the exact row(s).
        var groups = await dbContext.MethodTestLabNames
            .GroupBy(x => x.TestName.ToLower())
            .Where(g => g.Select(x => x.MethodTestId).Distinct().Count() > 1)
            .Select(g => new
            {
                LabName = g.Key,
                MethodNames = g.Select(x => x.MethodTest.Method.MethodName).Distinct().ToList()
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (groups.Count == 0)
        {
            return;
        }

        var errorMessages = groups
            .Select(group => $"{group.LabName} ({string.Join(", ", group.MethodNames)})")
            .ToList();

        var formattedErrorMessage = string.Join(Environment.NewLine, errorMessages);

        await sender.Send(new CreatePharosErrorCommand(PharosErrorCodes.DuplicateLaboratoryTestNamesDetected, ErrorClassification.Error, Parameters: [formattedErrorMessage]), cancellationToken);
    }
}