using MediatR;

namespace Pharos.Server.Application.Parameters;

/// <summary>
/// Checks whether a tube type is used by any quality control.
/// </summary>
/// <param name="TubeTypeId">Tube type identifier to check.</param>
public record IsTubeTypeUsedByAnyQualityControlQuery(int TubeTypeId) : IRequest<bool>;