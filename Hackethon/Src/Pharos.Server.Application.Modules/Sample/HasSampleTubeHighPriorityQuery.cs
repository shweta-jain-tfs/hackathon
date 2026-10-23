using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Sample;

/// <summary>
/// Query to check if any tube associated with a Request UID has high priority set.
/// </summary>
/// <param name="RequestUid">The request UID to check for high priority tubes.</param>
public record HasSampleTubeHighPriorityQuery(RequestUid RequestUid) : IRequest<bool>;
