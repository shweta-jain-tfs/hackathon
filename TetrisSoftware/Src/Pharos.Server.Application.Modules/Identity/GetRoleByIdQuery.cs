using MediatR;

namespace Pharos.Server.Application.Identity;
public record GetRoleByIdQuery(Guid RoleId, bool WithPermissions = false) : IRequest<RoleDto>;
