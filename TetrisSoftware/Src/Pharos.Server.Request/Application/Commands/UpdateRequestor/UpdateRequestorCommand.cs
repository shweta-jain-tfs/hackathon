using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Domain.Common;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Commands.UpdateRequestor;

public class UpdateRequestorCommand : IRequest
{
    [JsonIgnore]
    public int Id { get; set; }
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
    public DateTimeOffset LastModified { get; set; }
}

internal sealed class UpdateRequestorCommandHandler : IRequestHandler<UpdateRequestorCommand>
{
    private readonly IRequestDbContext _context;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptionsSnapshot<RequestSettings> _requestSettings;

    public UpdateRequestorCommandHandler(IRequestDbContext context, IAuthorizationService authorizationService, IOptionsSnapshot<RequestSettings> requestSettings)
    {
        _context = context;
        _authorizationService = authorizationService;
        _requestSettings = requestSettings;
    }

    public async Task Handle(UpdateRequestorCommand request, CancellationToken cancellationToken)
    {
        var personalAndHealthPermissions = await _authorizationService.AuthorizeCurrentUserAsync(Permissions.ViewPersonalAndHealthData);

        var canHandleRequestorInfo =
            _requestSettings.Value.IsRequestorInformationEnabled &&
            personalAndHealthPermissions;

        if (!canHandleRequestorInfo)
        {
            throw new UnauthorizedAccessException("User is not authorized to update requestors.");
        }

        var requestor = await _context.Requestors.FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (requestor == null)
        {
            throw new NotFoundException(nameof(Requestor), request.Id);
        }

        //TODO, some sort of merge is needed
        // Check for optimistic concurrency - if LastModified has changed since we loaded it
        //if (requestor.LastModified > request.LastModified)
        //{
        //    throw new ConflictException("The requestor has been modified by another user. Please refresh and try again.");
        //}

        // Check if another requestor with the same RequestorId already exists (excluding current one)
        var existingRequestor = await _context.Requestors
            .FirstOrDefaultAsync(r => r.RequestorId == request.RequestorId && r.Id != request.Id, cancellationToken);

        if (existingRequestor != null)
        {
            throw new ValidationException($"Requestor with ID '{request.RequestorId}' already exists.");
        }

        // Update properties
        requestor.RequestorId = request.RequestorId;
        requestor.Name = request.Name;
        requestor.Address = request.Address;
        requestor.Comment = request.Comment;
        requestor.ZipCode = request.ZipCode;
        requestor.OrderingPhysicianCode = request.OrderingPhysicianCode;
        requestor.PhoneNumber = request.PhoneNumber;
        requestor.ReflexEnabled = request.ReflexEnabled;
        requestor.ShortcutCode = request.ShortcutCode;

        await _context.SaveChangesAsync(cancellationToken);
    }
}