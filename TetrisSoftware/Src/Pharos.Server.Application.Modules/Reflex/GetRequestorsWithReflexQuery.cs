using MediatR;

namespace Pharos.Server.Application.Reflex;

/// <summary>
/// Gets a list of requestor ids that is assigned to reflex definitions. The list back will be a subset (or all) of RequestorIds
/// </summary>
/// <param name="RequestorIds">Request ids to check</param>
public record GetRequestorsWithReflexQuery(List<string> RequestorIds) : IRequest<List<string>>;