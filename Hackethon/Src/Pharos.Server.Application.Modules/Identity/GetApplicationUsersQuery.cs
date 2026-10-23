using MediatR;

namespace Pharos.Server.Application.Identity;

public record GetApplicationUsersQuery() : IRequest<List<ApplicationUserDto>>;