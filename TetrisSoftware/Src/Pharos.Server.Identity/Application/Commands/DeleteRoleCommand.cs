using System.ComponentModel.DataAnnotations;
using FluentValidation.Results;
using MediatR;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public record DeleteRoleCommand(Guid RoleId) : IRequest<bool>;

internal sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IApplicationRoleManager _applicationRoleManager;

    public DeleteRoleCommandHandler(IApplicationRoleManager applicationRoleManager)
    {
        _applicationRoleManager = applicationRoleManager;
    }

    public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = _applicationRoleManager.Roles.FirstOrDefault(x => x.Id == request.RoleId);
        if (role != null)
        {
            if (role.IsBuiltInRole)
            {
                throw new ValidationException(Translations.CannotEditBuiltInRole);
            }

            var result = await _applicationRoleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                var errors = new List<ValidationFailure>();
                foreach (var error in result.Errors)
                {
                    errors.Add(new ValidationFailure(error.Code, error.Description));
                }

                throw new FluentValidation.ValidationException(errors);
            }

            return true;
        }
        else
        {
            throw new ObjectGoneException(Translations.ObjectGone);
        }
    }
}
