using MediatR;
using Pharos.Server.Domain.Entities;

namespace Pharos.Server.Application.Request;

/// <summary>
/// This query is used to find requests for a given list of sample IDs.
/// For the found requests the query return RequestUid and a list of test order states.
/// </summary>
/// <param name="SampleIds">A set of sample IDs.</param>
public record GetTestOrderStatesQuery(HashSet<string> SampleIds) : IRequest<IReadOnlyDictionary<string, GetTestOrderStatesQueryResultItem>>;

public record GetTestOrderStatesQueryResultItem(RequestUid RequestUid, List<ITestOrder> TestOrderStates);
