using FluentValidation;
using MediatR;
using Pharos.Contracts.Parameters;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Localization;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Request.Application.Commands.RetestRejectedResultsCommand;

internal sealed class RetestRejectedResultsCommandHandler(
    IRequestDbContext dbContext,
    IMediator mediator,
    IMethodsService methodService) : IRequestHandler<Server.Application.Request.RetestRejectedResultsCommand>
{
    public async Task Handle(Server.Application.Request.RetestRejectedResultsCommand command, CancellationToken cancellationToken)
    {
        // This command is called from the Result module, when the user rejects test results. The test result is already rejected at this point,
        // but the TestResultStatusChangedEvent is not published yet, so the test order status must still be PendingApproval.
        // The TestResultStatusChangedEvent is handled later (after this pipeline is complete).
        var statesToInclude = new List<TestOrderState> { TestOrderState.PendingApproval, TestOrderState.PendingBlank, TestOrderState.PendingCalibration, TestOrderState.Aborted, TestOrderState.Flagged };
        var requestIds = await dbContext.TestOrders.Where(x => command.TestOrderIds.Contains(x.Uid) && statesToInclude.Contains(x.State))
            .Select(x => x.RequestId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        if (requestIds.Count == 0)
        {
            throw new NotFoundException("Could not find any requests for the provided test order IDs.");
        }

        foreach (var requestId in requestIds)
        {
            var request = await dbContext.Requests.Where(x => x.Id == requestId)
                .Include(x => x.Requestor)
                .Include(x => x.TestOrders)
                .ThenInclude(x => x.ReflexDefinitions)
                .Include(x => x.TestOrders)
                .ThenInclude(x => x.TestOrderCustomParameters)
                .FirstOrDefaultAsync(cancellationToken);

            if (request == null)
            {
                continue;
            }

            var testOrdersToCopy = request.TestOrders.Where(x => command.TestOrderIds.Contains(x.Uid)).ToList();

            foreach (var testOrder in testOrdersToCopy)
            {
                var instrumentDilution = command.InstrumentDilution ?? testOrder.InstrumentDilutionFactor;

                // Validate dilution
                var (preventDilutionFactorsBelowDefault, defaultDilutionFactor, technology) = await methodService.GetDefaultInstrumentDilutionFactor(testOrder.TestName, testOrder.MethodName);

                var validInstrumentDilutionFactors = GetValidDilutionFactors(technology);

                if (!validInstrumentDilutionFactors.Contains(instrumentDilution))
                {
                    throw new ValidationException(Translations.InvalidInstrumentDilutionFactor);
                }

                if (preventDilutionFactorsBelowDefault && instrumentDilution * request.PreDilutionFactor < defaultDilutionFactor)
                {
                    throw new ValidationException(Translations.CombinedDilutionFactorBelowMinimum);
                }

                var newTestOrder = new TestOrder
                {
                    Uid = Guid.NewGuid(),
                    State = TestOrderState.NotStarted,
                    EntityState = EntityState.Added,
                    MethodName = testOrder.MethodName,
                    TestName = testOrder.TestName,
                    NumberOfReplicates = testOrder.NumberOfReplicates,
                    InstrumentDilutionFactor = instrumentDilution,
                    RequestId = testOrder.RequestId,
                    LabTestName = testOrder.LabTestName,
                    TestSortOrder = request.TestOrders.Any() ? request.TestOrders.Max(x => x.TestSortOrder) + 1 : 1,
                    LisChannelId = testOrder.LisChannelId,
                    TestPanelIds = testOrder.TestPanelIds,
                    IsReflexTest = testOrder.IsReflexTest,
                    Request = testOrder.Request,
                    TestOrderCustomParameters = testOrder.TestOrderCustomParameters,
                    InstrumentNumber = null,
                    TubeId = null
                };

                //if the test order that is about to be rejected and retested has reflex definitions, we need to add them to the new test order
                if (testOrder.HasReflexTests)
                {
                    foreach (var testOrderReflexDefinition in testOrder.ReflexDefinitions!)
                    {
                        testOrderReflexDefinition.TestOrderId = newTestOrder.Uid;
                    }
                }

                request.TestOrders.Add(newTestOrder);
            }

            request.UpdateRequestState();
            await dbContext.SaveChangesAsync(cancellationToken);

            await mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), cancellationToken);

            await mediator.Send(new SyncTestOrdersWithInstrumentCommand(request), cancellationToken);
        }
    }

    private static List<int> GetValidDilutionFactors(AssayTechnology? technology)
    {
        if (!technology.HasValue)
        {
            return [];
        }

        return technology switch
        {
            AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap,
            AssayTechnology.EliA => InstrumentDilutionFactors.Elia,
            AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG,
            AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy,
            _ => throw new ArgumentOutOfRangeException(nameof(technology), technology, null)
        };
    }
}