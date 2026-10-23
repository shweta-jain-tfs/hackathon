using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Domain.Reflex;
using Pharos.Server.Request.Application.Commands.ProcessLisExport;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Domain;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Request.Application.Events;

internal class TestResultStatusChangedEventHandler(
    IRequestDbContext dbContext,
    IMediator mediator,
    ITestDisplayNameProvider displayNameProvider,
    IOptionsSnapshot<RequestSettings> requestSettings) : INotificationHandler<TestResultStatusChangedEvent>
{
    public async Task Handle(TestResultStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var testOrderIds = notification.TestResults.Select(x => x.TestOrderId).ToList();

        // Determine which test orders exist in DB and which are missing.
        var existingTestOrderUids = await dbContext.TestOrders
            .Where(sto => testOrderIds.Contains(sto.Uid))
            .Select(sto => sto.Uid)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var missingTestOrderIds = testOrderIds.Except(existingTestOrderUids).ToList();

        if (missingTestOrderIds.Count > 0)
        {
            await ReportMissingTestOrderErrorsAsync(missingTestOrderIds, notification.TestResults, cancellationToken).ConfigureAwait(false);
        }

        // If none of the test orders exist we have nothing more to update and should return.
        if (existingTestOrderUids.Count == 0)
        {
            return;
        }

        // Get requests to which the existing test orders belong so we can update request statuses.
        // We need all test orders in those requests (not just approved) to get correct request statuses.
        var requestIds = await dbContext.TestOrders
            .Where(sto => existingTestOrderUids.Contains(sto.Uid))
            .Select(sto => sto.RequestId)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var requests = await dbContext.Requests
                    .Where(x => requestIds.Contains(x.Id))
                    .Include(x => x.Requestor)
                    .Include(x => x.TestOrders)
                    .ThenInclude(x => x.ReflexDefinitions!)
                    .ThenInclude(x => x.ReflexTests)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

        var hasApprovedOrRejectedTestOrders = false;

        foreach (var request in requests)
        {
            var requestUpdated = false;
            var reflexInformationErrors = new List<CreatePharosErrorCommand>();
            var originalTestOrderCount = request.TestOrders.Count;
            for (var i = 0; i < originalTestOrderCount; i++)  // cannot use foreach, because we add reflex test orders
            {
                var testOrder = request.TestOrders[i];
                var testResult = notification.TestResults.FirstOrDefault(x => x.TestOrderId == testOrder.Uid);
                if (testResult == null)
                {
                    // Skip test orders that are not in the notification list
                    continue;
                }

                var newTestOrderState = testResult.Status switch
                {
                    TestResultStatus.PendingBlank => TestOrderState.PendingBlank,
                    TestResultStatus.PendingCalibration => TestOrderState.PendingCalibration,
                    TestResultStatus.PendingApproval => TestOrderState.PendingApproval,
                    TestResultStatus.Approved => TestOrderState.Approved,
                    TestResultStatus.ApprovedPendingReflex => TestOrderState.ApprovedPendingReflex,
                    TestResultStatus.Rejected => TestOrderState.Rejected,
                    TestResultStatus.Flagged => TestOrderState.Flagged,
                    TestResultStatus.Aborted => TestOrderState.Aborted,
                    TestResultStatus.Reported => TestOrderState.Reported,
                    _ => testOrder.State // Keep the existing status if not matched
                };

                if (testOrder.State == newTestOrderState)
                {
                    // no changes in status, skip
                    continue;
                }
                requestUpdated = true;
                testOrder.State = newTestOrderState;
                testOrder.EntityState = EntityState.Modified;

                hasApprovedOrRejectedTestOrders |= testOrder.State is TestOrderState.Approved or TestOrderState.Rejected;

                if (testOrder is { State: TestOrderState.Approved, ReflexDefinitions.Count: > 0 })
                {
                    await EvaluateReflexConditionsAndAddTests(testOrder, testResult, reflexInformationErrors, cancellationToken);
                }

                if (testOrder is { State: TestOrderState.Rejected, ReflexDefinitions.Count: > 0 })
                {
                    RemoveReflexConditions(testOrder);
                }
            }

            if (requestUpdated)
            {
                request.UpdateRequestState();
                await dbContext.SaveChangesAsync(cancellationToken);

                foreach (var reflexInformationError in reflexInformationErrors)
                {
                    await mediator.Send(reflexInformationError, cancellationToken);
                }

                await mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), cancellationToken);

                if (request.TestOrders.Any(x => x.EntityState == EntityState.Added))
                {
                    // send added reflex tests to instruments
                    await mediator.Send(new SyncTestOrdersWithInstrumentCommand(request), cancellationToken);
                }
            }
        }

        // Delegate automatic exports to dedicated command handler
        if (hasApprovedOrRejectedTestOrders)
        {
            var triggeredTestOrderUids = notification.TestResults
                .Where(tr => tr.Status is TestResultStatus.Approved or TestResultStatus.Rejected)
                .Select(tr => tr.TestOrderId)
                .ToList();

            await mediator.Send(new ProcessLisExportCommand(requests, triggeredTestOrderUids), cancellationToken);
        }
    }

    private static void RemoveReflexConditions(TestOrder testOrder)
    {
        testOrder.ReflexDefinitions?.Clear();
    }

    private async Task EvaluateReflexConditionsAndAddTests(TestOrder testOrder, ITestResult testResult, List<CreatePharosErrorCommand> reflexInformationErrors, CancellationToken cancellationToken)
    {
        for (var i = 0; i < testOrder.ReflexDefinitions!.Count;)
        {
            var reflexDefinition = testOrder.ReflexDefinitions[i];
            var requestorId = testOrder.Request.Requestor?.RequestorId;
            var requestor = testOrder.Request.Requestor;

            // If the reflex definition has a requestor ID and it does not match the requestor ID of the test order, or if the requestor does not have reflex enabled, remove the reflex definition from the list of reflex definitions.
            if ((reflexDefinition.RequestorId != null && reflexDefinition.RequestorId != requestorId) || (requestor != null && !requestor.ReflexEnabled))
            {
                dbContext.ReflexDefinitions.Remove(reflexDefinition);
                testOrder.ReflexDefinitions.RemoveAt(i);
                continue;
            }

            // If the reflex definition has no reflex tests or the reflex condition is not true for the test result, remove it from the list of reflex definitions.
            if (reflexDefinition.ReflexTests.Count == 0 || !reflexDefinition.ReflexCondition.IsTrueFor(testResult))
            {
                dbContext.ReflexDefinitions.Remove(reflexDefinition);
                testOrder.ReflexDefinitions.RemoveAt(i);
                continue;
            }

            if (reflexDefinition.Automatic)
            {
                // Process automatic reflex tests immediately
                var request = testOrder.Request;
                var addedTests = new List<string>();

                var sortedReflexTests = reflexDefinition.ReflexTests.OrderBy(x => x.TestSortOrder).ToList();
                for (var j = 0; j < sortedReflexTests.Count; j++)
                {
                    var reflexTest = sortedReflexTests[j];
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
                    newTest.TestSortOrder = j + 100; // we are not fetching all tests in the request, so we add a large offset to avoid conflicts with existing test sort orders
                    newTest.LabTestName = await displayNameProvider.GetTestDisplayName(reflexTest.MethodName, reflexTest.TestName);
                    newTest.EntityState = EntityState.Added;

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
                    reflexInformationErrors.Add(ReflexPharosErrors.AutomaticFollowUpTestsAdded(
                        reflexDefinition.Name,
                        request.SampleId,
                        request.RequestId ?? string.Empty,
                        FormatTest(testOrder.MethodName, testOrder.TestName),
                        addedTests));
                }

                dbContext.ReflexDefinitions.Remove(reflexDefinition);
                testOrder.ReflexDefinitions.RemoveAt(i);
            }
            else
            {
                // Mark the test order as pending reflex action (for manual processing later)
                testOrder.State = TestOrderState.ApprovedPendingReflex;

                i++; // only increment if we did not remove the reflex definition
            }
        }
    }

    private static string FormatTest(string methodName, string testName) => $"{methodName}/{testName}";

    private async Task ReportMissingTestOrderErrorsAsync(IEnumerable<Guid> missingTestOrderIds, IEnumerable<ITestResult> testResults, CancellationToken cancellationToken)
    {
        // Create an individual Pharos error for each missing test result so operators can act per sample/result.
        foreach (var missingUid in missingTestOrderIds)
        {
            var testResult = testResults.FirstOrDefault(tr => tr.TestOrderId == missingUid);
            if (testResult == null)
            {
                continue;
            }

            var sampleId = testResult.SampleId;

            // Build detail string as sampleId/methodName/testName
            var detailParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sampleId))
            {
                detailParts.Add(sampleId);
            }

            if (!string.IsNullOrWhiteSpace(testResult.MethodName))
            {
                detailParts.Add(testResult.MethodName);
            }

            if (!string.IsNullOrWhiteSpace(testResult.TestName))
            {
                detailParts.Add(testResult.TestName);
            }

            var detail = detailParts.Count > 0 ? string.Join("/", detailParts) : "Unknown result";
            var parameters = new List<string> { detail };
            var command = new CreatePharosErrorCommand(PharosErrorCodes.MissingRequestOrTestOrder, ErrorClassification.Warning, InstrumentNumber: null, Parameters: parameters);
            await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        }
    }
}
