using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Common;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.CreateRequestor;

public class CreateRequestorCommand : IRequest<int>
{
    public string RequestorId { get; set; } = string.Empty;
    [ProtectedData] public string? Name { get; set; }
    [ProtectedData] public string? Address { get; set; }
    [ProtectedData] public string? Comment { get; set; }
    public string? ZipCode { get; set; }
    public string? OrderingPhysicianCode { get; set; }
    public string? PhoneNumber { get; set; }
    public bool ReflexEnabled { get; set; } = true;
    public string? ShortcutCode { get; set; }
    public DateTimeOffset? LatestSampleDate { get; set; }
}

internal sealed class CreateRequestorCommandHandler : IRequestHandler<CreateRequestorCommand, int>
{
    private readonly IRequestDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;

    public CreateRequestorCommandHandler(IRequestDbContext context, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings)
    {
        _context = context;
        _authorizationService = authorizationService;
        _requestSettings = requestSettings;
    }

    public async Task<int> Handle(CreateRequestorCommand request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (!canHandleRequestorInfo)
        {
            throw new UnauthorizedAccessException("User is not authorized to create requestors.");
        }

        // Check if requestor with same RequestorId already exists
        var existingRequestor = await _context.Requestors
            .FirstOrDefaultAsync(r => r.RequestorId == request.RequestorId, cancellationToken);

        if (existingRequestor != null)
        {
            throw new ValidationException($"Requestor with ID '{request.RequestorId}' already exists.");
        }

        var requestor = new Requestor
        {
            RequestorId = request.RequestorId,
            Name = request.Name,
            Address = request.Address,
            Comment = request.Comment,
            ZipCode = request.ZipCode,
            OrderingPhysicianCode = request.OrderingPhysicianCode,
            PhoneNumber = request.PhoneNumber,
            ReflexEnabled = request.ReflexEnabled,
            ShortcutCode = request.ShortcutCode,
            LatestSampleDate = request.LatestSampleDate,
            NumberOfReportedSamples = 0,
            NumberOfReportedTests = 0,
        };

        _context.Requestors.Add(requestor);
        await _context.SaveChangesAsync(cancellationToken);

        return requestor.Id;
    }
}