using MediatR;

namespace Pharos.Server.Application.Identity;

public record GetRolesQuery() : IRequest<List<RoleDto>>;
