using MediatR;

namespace Pharos.Server.Application.Request;

public record CleanupRequestsCommand(int OlderThanDays) : IRequest;