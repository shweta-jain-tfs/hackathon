using MediatR;

namespace Pharos.Server.Application.Request;

/// <summary>
/// Checks whether a tube type is used by any request sample.
/// </summary>
/// <param name="TubeTypeId">Tube type identifier to check.</param>
public record IsTubeTypeUsedByAnyRequestQuery(int TubeTypeId) : IRequest<bool>;
