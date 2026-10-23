using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.UpdateRequestor;

public record ResetRequestorCountersCommand(int Id) : IRequest;

internal sealed class ResetRequestorCountersCommandHandler : IRequestHandler<ResetRequestorCountersCommand>
{
    private readonly IRequestDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;
    private readonly IDateTime _dateTime;

    public ResetRequestorCountersCommandHandler(IRequestDbContext context, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings, IDateTime dateTime)
    {
        _context = context;
        _authorizationService = authorizationService;
        _requestSettings = requestSettings;
        _dateTime = dateTime;
    }

    public async Task Handle(ResetRequestorCountersCommand request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (!canHandleRequestorInfo)
        {
            throw new UnauthorizedAccessException("User is not authorized to reset requestor counters.");
        }

        var requestor = await _context.Requestors.FirstOrDefaultAsync(r => r.Id == request.Id ,cancellationToken);

        if (requestor == null)
        {
            throw new NotFoundException(nameof(Requestor), request.Id);
        }

        requestor.NumberOfReportedSamples = 0;
        requestor.NumberOfReportedTests = 0;
        requestor.CounterResetDate = _dateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);
    }
}