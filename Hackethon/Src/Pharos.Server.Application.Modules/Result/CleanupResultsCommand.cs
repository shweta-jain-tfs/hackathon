using MediatR;

namespace Pharos.Server.Application.Result;

public record CleanupResultsCommand(int OlderThanDays) : IRequest;