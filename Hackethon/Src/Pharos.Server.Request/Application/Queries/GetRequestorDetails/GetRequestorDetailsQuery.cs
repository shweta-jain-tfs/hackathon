using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Request.Application.Interfaces;

namespace Pharos.Server.Request.Application.Queries.GetRequestorDetails;

public record GetRequestorDetailsQuery(int Id) : IRequest<RequestorDetailsDto?>;

internal sealed class GetRequestorDetailsQueryHandler : IRequestHandler<GetRequestorDetailsQuery, RequestorDetailsDto?>
{
    private readonly IRequestDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;

    public GetRequestorDetailsQueryHandler(IRequestDbContext context, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings)
    {
        _context = context;
        _authorizationService = authorizationService;
        _requestSettings = requestSettings;
    }

    public async Task<RequestorDetailsDto?> Handle(GetRequestorDetailsQuery request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (!canHandleRequestorInfo)
        {
            throw new UnauthorizedAccessException("User is not authorized to view requestor details.");
        }

        var requestor = await _context.Requestors
            .Where(x => x.Id == request.Id)
            .Select(r => new RequestorDetailsDto
            {
                Id = r.Id,
                RequestorId = r.RequestorId,
                Name = r.Name,
                Address = r.Address,
                Comment = r.Comment,
                ZipCode = r.ZipCode,
                OrderingPhysicianCode = r.OrderingPhysicianCode,
                PhoneNumber = r.PhoneNumber,
                ReflexEnabled = r.ReflexEnabled,
                ShortcutCode = r.ShortcutCode,
                LatestSampleDate = r.LatestSampleDate,
                NumberOfReportedSamples = r.NumberOfReportedSamples,
                NumberOfReportedTests = r.NumberOfReportedTests,
                CounterResetDate = r.CounterResetDate,
                LastModified = r.LastModified ?? DateTimeOffset.MinValue
            })
            .FirstOrDefaultAsync(cancellationToken);

        return requestor;
    }
}