using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pharos.Contracts;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.Identity;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Identity.Application.Commands;
using Pharos.Server.Identity.Domain;
using Pharos.Server.Identity.Infrastructure.Authorization;
using Pharos.Server.Identity.Web.Components.Account;
using IAuthorizationService = Pharos.Server.Application.Security.IAuthorizationService;

namespace Pharos.Server.Identity.Web;

[ApiController]
[Route("api/appusers")]
public class UsersController(
    ISender sender,
    ICurrentUserService currentUser,
    IApplicationUserManager userManager,
    SignInManager<ApplicationUser> signInManager,
    IAuthorizationService authorizationService,
    IOptions<IdentityOptions> identityOptions) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<UserInfo> GetMyUserInfo()
    {
        // we want to refresh the sign-in to ensure the latest claims are used
        var userId = currentUser.UserId ?? throw new InvalidOperationException("User ID is not available.");
        var user = await userManager.FindByGuidAsync(userId) ?? throw new UnauthorizedAccessException("User not found.");
        await signInManager.RefreshSignInAsync(user);

        var result = User.GetUserInfo(identityOptions.Value, authorizationService.RndMode) // the same function is used in PersistingServerAuthenticationStateProvider
            ?? throw new UnauthorizedAccessException(); // should not happen as [Authorize] is applied  

        return result;
    }

    [HttpGet("current")]
    [Authorize]
    public async Task<ApplicationUserDto?> GetCurrentUserDetails(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetCurrentUserDetailsQuery(), cancellationToken);
    }

    [HttpGet]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public async Task<IList<ApplicationUserDto>> GetApplicationUsers(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetApplicationUsersQuery(), cancellationToken);
    }

    [HttpGet("details/{id}")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public async Task<ApplicationUserDto?> GetApplicationUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await sender.Send(new GetApplicationUserByIdQuery(id), cancellationToken);
        if (user == null)
        {
            throw new NotFoundException();
        }
        return user;
    }

    [HttpPost("update")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public Task UpdateApplicationUser(UpdateApplicationUserCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("create")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public async Task<Guid> CreateApplicationUser(CreateApplicationUserCommand command, CancellationToken cancellationToken)
    {
        return await sender.Send(command, cancellationToken);
    }

    [HttpPost("changepassword")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public Task ChangePasswordApplicationUser(ChangePasswordApplicationUserCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("me/changepassword")]
    [Authorize]
    public async Task ChangeCurrentUserPassword(ChangeCurrentUserPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);

        var userId = currentUser.UserId ?? throw new InvalidOperationException("User ID is not available.");
        var user = await userManager.FindByGuidAsync(userId) ?? throw new UnauthorizedAccessException("User not found.");
        await signInManager.RefreshSignInAsync(user);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public Task DeleteApplicationUser(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteApplicationUserCommand(id);
        return sender.Send(command, cancellationToken);
    }
}
