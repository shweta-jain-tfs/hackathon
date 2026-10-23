using MediatR;
using Pharos.Server.Application.Lis;

namespace Pharos.Server.Application.Result;

/// <summary>
/// Retrieves test results for the specified test orders in a for prepared for sending them to LIS.
/// </summary>
public record GetSampleTestResultsForLisQuery(IReadOnlyList<Guid> TestOrderIds) : IRequest<List<LisSampleTestResult>>;
