using MediatR;

namespace Pharos.Server.Application.Result;

public record CleanupQualityCommand(int OlderThanDays) : IRequest;