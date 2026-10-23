using MediatR;

namespace Pharos.Server.Application.Identity;

/// <summary>
/// Query to retrieve the current logged-in user's account details.
/// </summary>
public record GetCurrentUserDetailsQuery : IRequest<ApplicationUserDto?>;
