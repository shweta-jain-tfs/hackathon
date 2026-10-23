using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Contracts.Notifications;
using Pharos.Server.SystemUpdate.Application.Interfaces;
using Pharos.Server.SystemUpdate.Domain;

namespace Pharos.Server.SystemUpdate.Application.Commands;

internal sealed record InitiateSystemUpdateCommand : IRequest;

internal sealed class InitiateSystemUpdateCommandHandler : IRequestHandler<InitiateSystemUpdateCommand>
{
    private readonly IUpdateServiceFactory _updateServiceFactory;
    private readonly IUpdateMediumStateStore _updateMediumStateStore;
    private readonly ISystemUpdateHub _updateHub;
    private readonly IUpdateOutcomeEvaluator _updateEvaluator;
    private readonly ILogger<InitiateSystemUpdateCommandHandler> _logger;

    public InitiateSystemUpdateCommandHandler(IUpdateServiceFactory updateServiceFactory,
        IUpdateMediumStateStore updateMediumStateStore,
        ISystemUpdateHub updateHub,
        IUpdateOutcomeEvaluator updateEvaluator,
        ILogger<InitiateSystemUpdateCommandHandler> logger)
    {
        _updateServiceFactory = updateServiceFactory;
        _updateMediumStateStore = updateMediumStateStore;
        _updateHub = updateHub;
        _updateEvaluator = updateEvaluator;
        _logger = logger;
    }
    public async Task Handle(InitiateSystemUpdateCommand request, CancellationToken cancellationToken)
    {
        var updateService = _updateServiceFactory.Resolve(_updateMediumStateStore.Get() is null ? UpdateSource.Remote : UpdateSource.Usb);

        await _updateHub.UpdateInProgress();

        try
        {
            await updateService.InitiateUpdate(cancellationToken);
        }
        catch (UpdateFailedException e)
        {
            _logger.LogError(e, "Update initiation failed.");

            // Tell clients that the update has failed so they can update their UI accordingly
            await _updateHub.UpdateFailed();

            await _updateEvaluator.EvaluateUpdateOutcome(cancellationToken);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogInformation(e, "Update process interrupted - application likely shutting down due to installation process.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred during update initiation.");
        }
    }
}
