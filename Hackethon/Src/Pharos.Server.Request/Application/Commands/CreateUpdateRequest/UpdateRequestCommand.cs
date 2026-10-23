using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Security;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Queries.GetRequestDetails;
using Pharos.Server.Request.Domain;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Request.Application.Commands.CreateUpdateRequest;

public class UpdateRequestCommand : BaseUpdateRequestCommand
{
    public int Id { get; set; }

    /// <summary>
    /// The date and time when the request details were queried. <see cref="RequestDetailsDto.QueriedAt"/>
    /// </summary>
    public DateTimeOffset RequestDetailsQueriedAt { get; set; }
}

internal sealed class UpdateRequestCommandHandler : BaseUpdateRequestCommandHandler<UpdateRequestCommand>
{
    private List<TestOrder>? _deletedTestOrders;

    public UpdateRequestCommandHandler(
        IAuthorizationService authorizationService,
        IRequestDbContext dbContext,
        IOptionsSnapshot<RequestSettings> requestSettings,
        IOptionsSnapshot<TubeTypesSettings> tubeSettings,
        IMediator mediator,
        IMethodsService methods) : base(authorizationService, dbContext, requestSettings, tubeSettings, mediator, methods)
    {
    }

    protected override async Task<Domain.Request> GetRequest(UpdateRequestCommand command, CancellationToken cancellationToken)
    {
        var query = DbContext.Requests
            .Where(s => s.Id == command.Id);

        query = query
            .Include(s => s.TestOrders)
            .ThenInclude(s => s.ReflexDefinitions!)
            .ThenInclude(s => s.ReflexTests);

        // TestOrderCustomParameters are needed for sending test orders to instruments (to avoid request re-fetching)
        query = query
            .Include(s => s.TestOrders)
            .ThenInclude(s => s.TestOrderCustomParameters);

        if (CanUpdatePatient)
        {
            query = query.Include(s => s.Patient);
        }

        if (CanUpdateRequestor)
        {
            query = query.Include(s => s.Requestor);
        }

        var request = await query
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (request == null)
        {
            // The request has been deleted since the client queried for details.
            throw new ObjectGoneException(Translations.DeletedRequest);
        }

        return request;
    }

    protected override bool ValidatePreDilutionAndSampleTubeType(Domain.Request request, UpdateRequestCommand command)
    {
        var dilutionChanged = base.ValidatePreDilutionAndSampleTubeType(request, command);

        var testsStarted = !request.TestOrders.All(to => to.State is TestOrderState.NotStarted or TestOrderState.Scheduled or TestOrderState.UnableToStart);
        if (testsStarted)
        {
            if (request.PreDilutionFactor != command.PreDilutionFactor)
            {
                throw new ValidationException("Changing sample pre-dilution factor is not allowed after tests have started.");
            }
            if (request.SampleTubeType != command.SampleTubeType)
            {
                throw new ValidationException("Changing sample tube type is not allowed after tests have started.");
            }
        }

        return dilutionChanged;
    }

    protected override async Task ValidateTests(Domain.Request request, UpdateRequestCommand command, bool preDilutionChanged)
    {
        var updatedTests = command.TestOrders!;
        foreach (var test in updatedTests)
        {
            if (test.Id <= 0)
            {
                continue;
            }

            var existingTestOrder = request.TestOrders.FirstOrDefault(to => to.Id == test.Id);
            if (existingTestOrder == null)
            {
                throw new ConflictException(Translations.ConflictingEdits);
            }

            if (existingTestOrder.State is not (TestOrderState.NotStarted or TestOrderState.Scheduled or TestOrderState.UnableToStart)
                && !test.IsReset
                && (existingTestOrder.InstrumentDilutionFactor != test.InstrumentDilutionFactor
                    || existingTestOrder.NumberOfReplicates != test.NumberOfReplicates
                    || existingTestOrder.IsActive != test.IsActive))
            {
                throw new ValidationException(Translations.CannotEditStartedTest);
            }
        }

        await base.ValidateTests(request, command, preDilutionChanged);
    }

    protected override async Task UpdateTestOrders(Domain.Request request, UpdateRequestCommand command, CancellationToken cancellationToken)
    {
        // Delete test orders that are no longer in the update, then add/update remaining test orders
        _deletedTestOrders = DeleteTestOrders(request, command);

        var updatedTests = command.TestOrders ?? [];
        if (updatedTests.Count == 0)
        {
            return;
        }

        var updatedTestOrderIdsInPendingReflexState = new List<Guid>();

        var existingTestOrders = request.TestOrders.ToList();

        var testSortOrder = 0;
        foreach (var order in updatedTests.OrderBy(x => x.TestSortOrder))
        {
            testSortOrder++;
            if (order.Id == 0) // new test
            {
                // Validation for adding duplicate test order as compared to the existing
                if (request.HasUnfinishedTestOrder(order.MethodName, order.TestName))
                {
                    throw new ValidationException(Translations.TestOrderDuplicate);
                }

                var to = await AddTestOrder(request, order, testSortOrder, cancellationToken);
                to.EntityState = EntityState.Added;
                continue;
            }

            // Find the existing test order
            var existingTestOrder = existingTestOrders.First(t => t.Id == order.Id);

            //always take the test panel ids, no matter the state of the test order
            existingTestOrder.TestPanelIds = order.TestPanelIds;

            // Handle reset: delete and recreate test order with new UID
            if (order.IsReset)
            {
                // Clone reflex definitions before removing the old test order to preserve them
                var clonedReflexDefinitions = new List<ReflexDefinition>();
                if (order.ReflexDefinitionIds != null)
                {
                    var reflexesToClone = existingTestOrder.ReflexDefinitions?.Where(x => order.ReflexDefinitionIds.Contains(x.Id)).Select(x => x).ToList();
                    clonedReflexDefinitions = TestOrderResetHelper.CloneReflexDefinitions(reflexesToClone);
                }

                // Remove the old test order from database and request
                DbContext.TestOrders.Remove(existingTestOrder);
                request.TestOrders.Remove(existingTestOrder);
                _deletedTestOrders.Add(existingTestOrder);

                // Create new test order with fresh UID, preserving all old values
                var resetTestOrder = TestOrderResetHelper.ResetTestOrder(existingTestOrder);

                // Update specific properties: TestSortOrder from list position and PreDilutionFactor from request level
                resetTestOrder.TestSortOrder = testSortOrder;
                resetTestOrder.ReflexDefinitions = clonedReflexDefinitions;

                request.TestOrders.Add(resetTestOrder);
                resetTestOrder.EntityState = EntityState.Added;
                continue;
            }

            if (existingTestOrder.TestSortOrder != testSortOrder || existingTestOrder.InstrumentDilutionFactor != order.InstrumentDilutionFactor
                || existingTestOrder.NumberOfReplicates != order.NumberOfReplicates || existingTestOrder.IsActive != order.IsActive)
            {
                existingTestOrder.TestSortOrder = testSortOrder;
                existingTestOrder.InstrumentDilutionFactor = order.InstrumentDilutionFactor;
                existingTestOrder.NumberOfReplicates = order.NumberOfReplicates;
                existingTestOrder.IsActive = order.IsActive;
                existingTestOrder.EntityState = EntityState.Modified;
            }

            // Remove reflex definitions that are no longer present in the updated test.
            // Our domain logic does not allow adding reflex definitions to existing test orders, but it allows removing them.
            if (existingTestOrder.ReflexDefinitions != null)
            {
                // Get the set of IDs that were present before
                var existingIds = existingTestOrder.ReflexDefinitions.Select(rd => rd.Id).ToHashSet();

                // Only keep those IDs that are both in the original and in the update
                var idsToKeep = existingIds.Intersect(order.ReflexDefinitionIds ?? []).ToHashSet();

                // Remove reflex definitions not present in the update
                var reflexesToRemove = existingTestOrder.ReflexDefinitions
                    .Where(rd => !idsToKeep.Contains(rd.Id))
                    .ToList();

                foreach (var reflexToRemove in reflexesToRemove)
                {
                    existingTestOrder.ReflexDefinitions.Remove(reflexToRemove);
                }

                // In this update flow, move back to Approved only when all reflex definitions have been removed.
                if (existingTestOrder.State == TestOrderState.ApprovedPendingReflex
                    && !existingTestOrder.ReflexDefinitions.Any())
                {
                    existingTestOrder.State = TestOrderState.Approved;
                    updatedTestOrderIdsInPendingReflexState.Add(existingTestOrder.Uid);
                }
            }
        }

        RecalculateTestOrder(request);

        if (updatedTestOrderIdsInPendingReflexState.Count > 0)
        {
            await Mediator.Publish(new PendingReflexTestsHandledEvent(updatedTestOrderIdsInPendingReflexState), cancellationToken);
        }

        request.UpdateRequestState();
    }

    protected override async Task PublishNotifications(Domain.Request request)
    {
        if (_deletedTestOrders is { Count: > 0 })
        {
            await Mediator.Publish(new TestOrdersDeletedEvent(_deletedTestOrders.Select(x => x.Uid).ToList()), CancellationToken.None);
        }

        // Notify other parts of the system when the request or its test orders have changed.
        if (RequestPropertiesChanged
            || _deletedTestOrders is { Count: > 0 }
            || request.TestOrders.Any(to => to.EntityState != EntityState.Unchanged))
        {
            if (TubePropertiesChanged)
            {
                foreach (var testOrder in request.TestOrders)
                {
                    if (testOrder.EntityState == EntityState.Unchanged)
                    {
                        testOrder.EntityState = EntityState.Modified;
                    }
                }
            }

            await Mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), CancellationToken.None);
        }

        // Update the instrument when test orders are added, changed, or removed.
        if (_deletedTestOrders is { Count: > 0 } || request.TestOrders.Any(to => to.EntityState != EntityState.Unchanged))
        {
            await Mediator.Send(new SyncTestOrdersWithInstrumentCommand(request, DeletedTestOrders: _deletedTestOrders));
        }
    }

    private List<TestOrder> DeleteTestOrders(Domain.Request request, UpdateRequestCommand command)
    {
        var existingTestOrders = request.TestOrders.ToList();

        // We do not want to delete test orders that were added after the request details were queried, even if they are not present in the update.
        // This can happen when multiple users are updating the same request at the same time.
        var deletedTestOrders = existingTestOrders
            .Where(x => x.Created < command.RequestDetailsQueriedAt && !(command.TestOrders?.Exists(y => y.Id == x.Id) ?? false))
            .ToList();

        // Remove the deleted test orders from the database and the request
        foreach (var deletedTestOrder in deletedTestOrders)
        {
            DbContext.TestOrders.Remove(deletedTestOrder);
            request.TestOrders.Remove(deletedTestOrder);
        }

        return deletedTestOrders;
    }

    /// <summary>
    /// This method is needed when we merge tests received from the client with the tests created after the request details were
    /// retried by the client app. Some test order values may collide.
    /// </summary>
    private static void RecalculateTestOrder(Domain.Request request)
    {
        var testOrders = request.TestOrders
            .OrderBy(x => x.TestSortOrder)
            .ThenBy(x => x.Id == 0 ? int.MaxValue : x.Id) // a test created by another user should go first (there can be no duplicate otherwise).
            .ToList();

        var order = 1;
        foreach (var testOrder in testOrders)
        {
            testOrder.TestSortOrder = order;
            order++;
        }
    }
}

