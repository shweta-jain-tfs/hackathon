using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;

namespace Pharos.Server.Request.Application.Commands;

/// <summary>
/// Command for adding pending reflex tests manually.
/// </summary>
public record HandlePendingReflexesCommand(IReadOnlyList<int> Ids, bool DoNotAddReflexTests) : IRequest;

internal sealed class HandlePendingReflexesCommandHandler(
    IRequestDbContext dbContext,
    IMediator mediator,
    ITestDisplayNameProvider displayNameProvider,
    IOptionsSnapshot<RequestSettings> requestSettings) : IRequestHandler<HandlePendingReflexesCommand>
{
    public async Task Handle(HandlePendingReflexesCommand command, CancellationToken cancellationToken)
    {
        // Find requests that have test orders in ApprovedPendingReflex state and that have reflex definitions matching the provided ids
        var requestIds = await dbContext.TestOrders
            .Where(x => x.State == TestOrderState.ApprovedPendingReflex && x.ReflexDefinitions!.Any(r => command.Ids.Contains(r.Id)))
            .Select(x => x.RequestId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (requestIds.Count == 0)
        {
            return;
        }

        var updatedRequests = new HashSet<Domain.Request>();
        var completedPendingReflexTestOrderIds = new List<Guid>();
        var reflexInformationErrors = new List<CreatePharosErrorCommand>();

        foreach (var requestId in requestIds)
        {
            var testOrders = await dbContext.TestOrders
                .Where(x => x.RequestId == requestId)
                .Include(x => x.Request)
                .ThenInclude(x => x.Requestor)
                .Include(x => x.ReflexDefinitions!)
                .ThenInclude(x => x.ReflexTests)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var testOrdersWithPendingReflex = testOrders
                .Where(x => x is { State: TestOrderState.ApprovedPendingReflex }
                            && x.ReflexDefinitions!.Any(r => command.Ids.Contains(r.Id)))
                .ToList();

            foreach (var testOrder in testOrdersWithPendingReflex)
            {
                var reflexDefinitionsToProcess = testOrder.ReflexDefinitions!.Where(x => command.Ids.Contains(x.Id))
                    .ToList();

                dbContext.ReflexDefinitions.RemoveRange(reflexDefinitionsToProcess);
                foreach (var reflexDefinition in reflexDefinitionsToProcess)
                {
                    testOrder.ReflexDefinitions!.Remove(reflexDefinition);
                }

                var allPendingReflexesHandled = testOrder.ReflexDefinitions!.Count == 0;

                testOrder.State = allPendingReflexesHandled
                    ? TestOrderState.Approved
                    : TestOrderState.ApprovedPendingReflex;

                if (allPendingReflexesHandled)
                {
                    completedPendingReflexTestOrderIds.Add(testOrder.Uid);
                }

                if (command.DoNotAddReflexTests)
                {
                    foreach (var reflexDefinition in reflexDefinitionsToProcess)
                    {
                        reflexInformationErrors.Add(ReflexPharosErrors.ManualFollowUpTestsDiscarded(
                            reflexDefinition.Name,
                            testOrder.Request.SampleId,
                            testOrder.Request.RequestId ?? string.Empty,
                            FormatTest(testOrder.MethodName, testOrder.TestName),
                            reflexDefinition.ReflexTests
                                .OrderBy(x => x.TestSortOrder)
                                .Select(x => FormatTest(x.MethodName, x.TestName))
                                .ToList()));
                    }

                    continue;
                }

                var requestor = testOrder.Request.Requestor;
                var request = testOrder.Request;

                foreach (var reflexDefinition in reflexDefinitionsToProcess)
                {
                    // Check requestor constraint if the reflex definition has a RequestorId set
                    if (!string.IsNullOrEmpty(reflexDefinition.RequestorId))
                    {
                        // Only add reflex tests if the request's RequestorId matches the reflex definition's RequestorId
                        if (requestor == null || requestor.RequestorId != reflexDefinition.RequestorId)
                        {
                            // Requestor constraint not fulfilled, skip this reflex definition
                            continue;
                        }
                    }

                    var sortedReflexTests = reflexDefinition.ReflexTests.OrderBy(x => x.TestSortOrder).ToList();
                    var addedTests = new List<string>();
                    for (var i = 0; i < sortedReflexTests.Count; i++)
                    {
                        var reflexTest = sortedReflexTests[i];
                        var shouldSkipReflexTest = requestSettings.Value.PreventDuplicateReflexTests
                            ? request.HasTestOrder(reflexTest.MethodName, reflexTest.TestName)
                            : request.HasUnfinishedTestOrder(reflexTest.MethodName, reflexTest.TestName);
                        if (shouldSkipReflexTest)
                        {
                            continue;
                        }

                        var newTest = reflexTest.ToTestOrder();
                        newTest.Uid = Guid.NewGuid();
                        newTest.IsReflexTest = true;
                        newTest.TestSortOrder = i + 100; // we are not fetching all tests in the request, so we add a large offset to avoid conflicts with existing test sort orders
                        newTest.LabTestName = await displayNameProvider.GetTestDisplayName(reflexTest.MethodName, reflexTest.TestName);
                        newTest.LisChannelId = testOrder.LisChannelId;

                        // Create fresh reflex definitions for the new reflex test
                        newTest.ReflexDefinitions =
                            await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
                                mediator,
                                newTest.TestName,
                                newTest.MethodName,
                                reflexDefinitionIds: null,
                                cancellationToken);

                        request.TestOrders.Add(newTest);
                        addedTests.Add(FormatTest(reflexTest.MethodName, reflexTest.TestName));
                    }

                    if (addedTests.Count > 0)
                    {
                        reflexInformationErrors.Add(ReflexPharosErrors.ManualFollowUpTestsAdded(
                            reflexDefinition.Name,
                            request.SampleId,
                            request.RequestId ?? string.Empty,
                            FormatTest(testOrder.MethodName, testOrder.TestName),
                            addedTests));
                    }
                }

                updatedRequests.Add(request);
            }
        }

        if (!dbContext.HasChanges())
        {
            return;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var reflexInformationError in reflexInformationErrors)
        {
            await mediator.Send(reflexInformationError, cancellationToken);
        }

        if (completedPendingReflexTestOrderIds.Count > 0)
        {
            await mediator.Publish(new PendingReflexTestsHandledEvent(completedPendingReflexTestOrderIds), cancellationToken);
        }

        foreach (var request in updatedRequests)
        {
            await mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), cancellationToken).ConfigureAwait(false);
            await mediator.Send(new SyncTestOrdersWithInstrumentCommand(request), cancellationToken);
        }
    }

    private static string FormatTest(string methodName, string testName) => $"{methodName}/{testName}";
}
