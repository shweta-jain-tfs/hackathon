using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Identity;
using Pharos.Server.Identity.Application.Commands;

namespace Pharos.Server.Identity.Web;

[ApiController]
[Route("api/approles")]
public class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public async Task<IList<RoleDto>> GetRoles(CancellationToken cancellationToken)
    {
        return await sender.Send(new GetRolesQuery(), cancellationToken);
    }

    [HttpGet("details/{id}")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public async Task<RoleDto?> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        return await sender.Send(new GetRoleByIdQuery(id), cancellationToken);
    }

    [HttpPost("create")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public Task<Guid> CreateRole(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpPost("update")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public Task UpdateRole(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        return sender.Send(command, cancellationToken);
    }

    [HttpDelete("{id}")]
    [Authorize(Permissions.ManageUsersAndRoles)]
    public Task<bool> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        return sender.Send(new DeleteRoleCommand(id), cancellationToken);
    }
}
