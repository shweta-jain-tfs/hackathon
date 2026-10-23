using MediatR;

namespace Pharos.Server.Application.Result;

public record GetTestOrderIdsFromAnalyticalRunQuery(int AnalyticalRunId) : IRequest<IReadOnlyList<Guid>>;
