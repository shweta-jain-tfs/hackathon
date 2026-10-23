using MediatR;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Queries;

/// <summary>
/// Returns a list of available update package identifiers for the active medium.
/// If none are available, returns an empty list.
/// </summary>
internal sealed record GetAvailableUpdatesQuery : IRequest<List<string>>;

internal sealed class GetAvailableUpdatesQueryHandler : IRequestHandler<GetAvailableUpdatesQuery, List<string>>
{
    private readonly IUpdateServiceFactory _updateServiceFactory;
    private readonly IUpdateMediumStateStore _stateStore;

    public GetAvailableUpdatesQueryHandler(
        IUpdateServiceFactory updateServiceFactory,
        IUpdateMediumStateStore stateStore)
    {
        _updateServiceFactory = updateServiceFactory;
        _stateStore = stateStore;
    }

    public Task<List<string>> Handle(GetAvailableUpdatesQuery request, CancellationToken cancellationToken)
    {
        var state = _stateStore.Get();
        if (state is null || string.IsNullOrWhiteSpace(state.DriveName))
        {
            return Task.FromResult(new List<string>());
        }

        var usbService = _updateServiceFactory.Resolve(UpdateSource.Usb);

        try
        {
            var packages = usbService.GetAvailableUpdates(cancellationToken);
            return Task.FromResult(packages ?? new List<string>());
        }
        catch
        {
            // Any enumeration error -> treat as no updates.
            return Task.FromResult(new List<string>());
        }
    }
}