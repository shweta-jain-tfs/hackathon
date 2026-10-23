using FluentValidation.Results;
using MediatR;
using Pharos.Server.Identity.Infrastructure.Authorization;

namespace Pharos.Server.Identity.Application.Commands;

public record ChangePasswordApplicationUserCommand(Guid UserId, string Password) : IRequest;

internal sealed class ChangePasswordApplicationUserCommandHandler(IApplicationUserManager manager) : IRequestHandler<ChangePasswordApplicationUserCommand>
{
    public async Task Handle(ChangePasswordApplicationUserCommand request, CancellationToken cancellationToken)
    {
        var result = await manager.UpdateUserWithTemporaryPasswordAsync(request.UserId, request.Password);

        if (!result.Succeeded)
        {
            var errors = new List<ValidationFailure>();
            foreach (var error in result.Errors)
            {
                errors.Add(new ValidationFailure(error.Code, error.Description));
            }

            throw new FluentValidation.ValidationException(errors);
        }
    }
}
