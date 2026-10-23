using MediatR;

namespace Pharos.Server.Application.Identity;

public sealed record UserLoggedInEvent(Guid? UserId, string? InstrumentNumber = null) : INotification;