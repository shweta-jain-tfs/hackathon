using MediatR;
using Pharos.Server.Application.Reflex;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Helpers;

internal static class ReflexDefinitionHelper
{
    /// <summary>
    /// Fetches and filters reflex definitions for a test and method.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="testName"></param>
    /// <param name="methodName"></param>
    /// <param name="reflexDefinitionIds">
    /// IDs to filter by. When <c>null</c>, all available definitions are returned.
    /// When provided, only definitions whose ID appears in this list are returned;
    /// if no IDs match, an empty list is returned.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<List<ReflexDefinition>> CreateFreshReflexDefinitionsAsync(
        ISender sender,
        string testName,
        string methodName,
        List<int>? reflexDefinitionIds,
        CancellationToken cancellationToken)
    {
        // Fetch all reflex definitions for the given test/method
        var allReflexDefinitions = (await sender.Send(
                new GetReflexDefinitionsForTestQuery(testName, methodName),
                cancellationToken).ConfigureAwait(false))
            .ToList();

        // null means "use all"; a list means "filter by IDs" (returns empty if none match)
        var definitionsToUse = reflexDefinitionIds is null
            ? allReflexDefinitions
            : allReflexDefinitions.Where(rd => reflexDefinitionIds.Contains(rd.Id)).ToList();

        // Create fresh, mutable copies (no IDs)
        var freshCopies = definitionsToUse
            .Select(definition => new ReflexDefinition
            {
                Name = definition.Name,
                Automatic = definition.Automatic,
                RequestorId = definition.RequestorId,
                ReflexCondition = definition.ReflexCondition,
                ReflexTests = definition.ReflexTests
                    .Select(rt => new ReflexTest
                    {
                        TestName = rt.TestName,
                        MethodName = rt.MethodName,
                        NumberOfReplicates = rt.NumberOfReplicates,
                        InstrumentDilutionFactor = rt.InstrumentDilutionFactor,
                        TestSortOrder = rt.TestSortOrder
                    })
                    .ToList()
            })
            .ToList();

        return freshCopies;
    }
}