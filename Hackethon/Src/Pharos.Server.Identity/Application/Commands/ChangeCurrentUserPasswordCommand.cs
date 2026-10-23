using FluentValidation;
using MediatR;
using Pharos.Server.Application;
using Pharos.Server.Infrastructure;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Localization;

namespace Pharos.Server.Identity.Application.Commands;

public record ChangeCurrentUserPasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

internal sealed class ChangeCurrentUserPasswordCommandHandler(
    ICurrentUserService currentUserService,
    IApplicationUserManager userManager,
    IDateTime dateTime) : IRequestHandler<ChangeCurrentUserPasswordCommand>
{
    public async Task Handle(ChangeCurrentUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();
        var user = await userManager.FindByGuidAsync(userId) ?? throw new UnauthorizedAccessException();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException(Translations.PasswordRequired);
        }

        if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
        {
            throw new ValidationException(Translations.Login_InvalidUserNameOrPassword);
        }

        if (await userManager.CheckPasswordAsync(user, request.NewPassword))
        {
            throw new ValidationException(Translations.ChangePassword_PasswordsAreTheSame);
        }

        if (user.UserName is null || !userManager.IsPasswordValid(request.NewPassword, user.UserName))
        {
            throw new ValidationException(Translations.ChangePassword_PasswordComplexityViolation);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new ValidationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        user.TemporaryPassword = false;

        user.PasswordEmittedDateTimeOffset = dateTime.Now;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }
    }
}
