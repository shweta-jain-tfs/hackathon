using System.ComponentModel.DataAnnotations;
using FluentValidation.Results;
using MediatR;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public record CreateRoleCommand(string DisplayName) : IRequest<Guid>;

internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
{
    private readonly IApplicationRoleManager _applicationRoleManager;

    public CreateRoleCommandHandler(IApplicationRoleManager applicationRoleManager)
    {
        _applicationRoleManager = applicationRoleManager;
    }

    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // Validate DisplayName is not empty
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ValidationException(Translations.RoleNameRequired);
        }

        // Name is used by identity to identify the role in user table it must be unique for every role.
        // Pharos demands uniq display name but allows it to be changed.
        // All build in roles and predefined will have a predefine readable name. Pharos.Contracts.Security
        var role = new ApplicationRole
        {
            Name = Guid.NewGuid().ToString(),
            DisplayName = request.DisplayName.Trim()
        };
        var result = await _applicationRoleManager.CreateAsync(role); //updates role with Id

        if (!result.Succeeded)
        {
            var errors = new List<ValidationFailure>();
            foreach (var error in result.Errors)
            {
                errors.Add(new ValidationFailure(error.Code, error.Description));
            }

            throw new FluentValidation.ValidationException(errors);
        }

        return role.Id;
    }
}
