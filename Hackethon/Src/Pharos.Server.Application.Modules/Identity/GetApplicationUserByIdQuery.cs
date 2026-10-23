using MediatR;

namespace Pharos.Server.Application.Identity;

public record GetApplicationUserByIdQuery(Guid UserId) : IRequest<ApplicationUserDto?>;
