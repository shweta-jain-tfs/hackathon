using MediatR;

namespace Pharos.Server.Application.Request;

/// <summary>
/// Command that is sent to update the requestor counters for a list of test orders.
/// </summary>
public record UpdateRequestorCountersCommand(IReadOnlyList<Guid> TestOrderIds) : IRequest;
