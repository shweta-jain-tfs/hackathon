using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Command;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Commands.DeleteRequestor;

public record DeleteRequestorCommand(List<int> Ids) : IRequest<DeleteResponse>;

internal sealed class DeleteRequestorCommandHandler : IRequestHandler<DeleteRequestorCommand, DeleteResponse>
{
    private readonly IRequestDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;
    private readonly ISender _sender;

    public DeleteRequestorCommandHandler(IRequestDbContext context, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings, ISender sender)
    {
        _context = context;
        _authorizationService = authorizationService;
        _requestSettings = requestSettings;
        _sender = sender;
    }

    private record RequestorInfo(int Id, string RequestorId);

    public async Task<DeleteResponse> Handle(DeleteRequestorCommand request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (!canHandleRequestorInfo)
        {
            throw new UnauthorizedAccessException("User is not authorized to delete requestors.");
        }

        // Get all requestors that should be deleted
        var requestorsToCheck = await _context.Requestors
            .Where(r => request.Ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        // Check which requestors have connected requests
        var candidatesToBeDeleted = new List<RequestorInfo>();
        var requestorIdsToSkip = new List<int>();

        foreach (var requestor in requestorsToCheck)
        {
            var hasConnectedRequestor = await _context.Requests
                .AnyAsync(req => req.RequestorId.HasValue && req.RequestorId.Value == requestor.Id, cancellationToken);

            if (hasConnectedRequestor)
            {
                requestorIdsToSkip.Add(requestor.Id);
            }
            else
            {
                candidatesToBeDeleted.Add(new RequestorInfo(requestor.Id, requestor.RequestorId));
            }
        }

        // Based on the requestorIdsToDelete, find out which of them that does not have any reflex definitions
        var requestorsWithReflex = await _sender.Send(new GetRequestorsWithReflexQuery(candidatesToBeDeleted.Select(x => x.RequestorId).ToList()), cancellationToken);

        requestorIdsToSkip.AddRange(candidatesToBeDeleted.Where(x => requestorsWithReflex.Contains(x.RequestorId)).Select(x => x.Id));

        // Only delete requestors that are not used in reflex definitions
        candidatesToBeDeleted.RemoveAll(r => requestorsWithReflex.Contains(r.RequestorId));

        // Delete only requestors without connected requests and reflexes
        var requestorsToDelete = requestorsToCheck
            .Where(r => candidatesToBeDeleted.Any(x => x.Id == r.Id))
            .ToList();

        if (requestorsToDelete.Count > 0)
        {
            _context.Requestors.RemoveRange(requestorsToDelete);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new DeleteResponse
        {
            Deleted = requestorsToDelete.Count,
            Skipped = requestorIdsToSkip.Count,
            SkippedIds = requestorIdsToSkip.ToList()
        };
    }
}