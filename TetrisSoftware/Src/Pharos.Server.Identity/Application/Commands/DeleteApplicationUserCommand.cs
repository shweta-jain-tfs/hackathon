
using System.ComponentModel.DataAnnotations;
using FluentValidation.Results;
using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Identity.Infrastructure.Hubs;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public record DeleteApplicationUserCommand(Guid UserId) : IRequest;

internal sealed class DeleteApplicationUserCommandHandler : IRequestHandler<DeleteApplicationUserCommand>
{
    private readonly IApplicationUserManager _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentUserServerHub _currentUserHub;

    public DeleteApplicationUserCommandHandler(
        IApplicationUserManager userManager,
        ICurrentUserService currentUserService,
        ICurrentUserServerHub currentUserHub)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _currentUserHub = currentUserHub;
    }

    public async Task Handle(DeleteApplicationUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId == request.UserId)
        {
            throw new ValidationException(Translations.CannotDeleteCurrentUser);
        }

        var result = await _userManager.DeleteUserAsync(request.UserId);

        if (!result.Succeeded)
        {
            var errors = new List<ValidationFailure>();
            foreach (var error in result.Errors)
            {
                errors.Add(new ValidationFailure(error.Code, error.Description));
            }

            throw new FluentValidation.ValidationException(errors);
        }

        // Notify the current user hub about the deletion: refresh triggers logout
        // for the deleted user if they are currently logged in.
        await _currentUserHub.UserInfoChanged(request.UserId);
    }
}
