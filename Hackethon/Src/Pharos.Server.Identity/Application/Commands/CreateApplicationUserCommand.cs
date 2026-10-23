using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Pharos.Contracts.Security;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public record CreateApplicationUserCommand(string UserName, string Password) : IRequest<Guid>;

internal sealed class CreateApplicationUserCommandHandler : IRequestHandler<CreateApplicationUserCommand, Guid>
{
    private readonly IUserStore<ApplicationUser> _userStore;
    private readonly IApplicationUserManager _userManager;

    public CreateApplicationUserCommandHandler(
        IUserStore<ApplicationUser> userStore,
        IApplicationUserManager userManager)
    {
        _userStore = userStore;
        _userManager = userManager;
    }

    public static void ValidateUsername(string username)
    {
        if (string.IsNullOrEmpty(username) || username.Length > UserContract.MaximumNameLength)
        {
            throw new ValidationException(Translations.InvalidUsername);
        }

        var allowedChars = new HashSet<char>(UserContract.AllowedNameCharacters);
        foreach (var c in username)
        {
            if (!allowedChars.Contains(c))
            {
                throw new ValidationException(Translations.InvalidUsername);

            }
        }
    }

    public async Task<Guid> Handle(CreateApplicationUserCommand request, CancellationToken cancellationToken)
    {
        ValidateUsername(request.UserName);

        var user = new ApplicationUser();
        await _userStore.SetUserNameAsync(user, request.UserName, cancellationToken);
        user.TemporaryPassword = true;
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = new List<ValidationFailure>();
            foreach (var error in result.Errors)
            {
                errors.Add(new ValidationFailure(error.Code, error.Description));
            }
            throw new ValidationException(errors);
        }

        return user.Id;
    }
}
