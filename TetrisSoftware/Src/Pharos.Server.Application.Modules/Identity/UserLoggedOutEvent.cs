using MediatR;

namespace Pharos.Server.Application.Identity;

public sealed record UserLoggedOutEvent(int? InstrumentId = null) : INotification;
