using MediatR;
using Pharos.Contracts.Notifications;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;

namespace Pharos.Server.Settings.Application.Commands.NotifySettingsChanged;

internal sealed record NotifySettingsChangedCommand(
    IReadOnlyCollection<string>? UpdatedKeys = null,
    IReadOnlyCollection<string>? DeletedKeys = null) : IRequest;

internal sealed class NotifySettingsChangedCommandHandler(
    ISettingsHub settingsHub,
    ICurrentUserService currentUserService,
    ISender sender) : IRequestHandler<NotifySettingsChangedCommand>
{
    public async Task Handle(NotifySettingsChangedCommand request, CancellationToken cancellationToken)
    {
        var updatedKeys = NormalizeKeys(request.UpdatedKeys);
        var deletedKeys = NormalizeKeys(request.DeletedKeys);
        var changedBy = await GetCurrentUserDisplayName(cancellationToken).ConfigureAwait(false);

        await settingsHub.SettingsChanged(updatedKeys, deletedKeys, changedBy).ConfigureAwait(false);
    }

    private async Task<string?> GetCurrentUserDisplayName(CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } userId)
        {
            return null;
        }

        var user = await sender.Send(new GetApplicationUserByIdQuery(userId), cancellationToken)
            .ConfigureAwait(false);

        return !string.IsNullOrWhiteSpace(user?.FullName)
            ? user.FullName
            : user?.UserName;
    }

    private static List<string> NormalizeKeys(IEnumerable<string>? keys)
    {
        return keys?
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];
    }
}
