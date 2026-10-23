using Google.Protobuf.WellKnownTypes;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Mapping;
using Pharos.Server.Request.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.TestOrder;
using EntityState = Pharos.Server.Application.Request.EntityState;

namespace Pharos.Server.Request.Application.Commands.TestOrderScheduling;

/// <summary>
/// Send this command to synchronize the test orders of a request with the instrument based on the current state of the request.
/// If the request has no modifications, only the not started test orders will be sent to the instrument.
/// </summary>
/// <param name="Request">The request with included TestOrders and TestOrderCustomParameters.</param>
/// <param name="DeletedTestOrders">The deleted test orders (only available after request modification).</param>
/// <param name="SampleTube">The sample tube that is being processed. If provided, only the test orders related to this tube will be sent to the instrument.</param>
/// <param name="PublishRequestUpdatedEvent">
/// Indicates whether to publish RequestUpdatedEvent after synchronization. We don't need to publish the event, for example, when we are editing the request in the UI (to avoid duplication).
/// </param>
internal record SyncTestOrdersWithInstrumentCommand(Domain.Request Request, List<Domain.TestOrder>? DeletedTestOrders = null, SampleTubeDto? SampleTube = null, bool PublishRequestUpdatedEvent = false) : IRequest;

internal sealed class SyncTestOrdersWithInstrumentCommandHandler(
    IRequestDbContext dbContext,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    IMethodsService methods,
    IArticlesService articles,
    IMediator mediator,
    ILogger<SyncTestOrdersWithInstrumentCommandHandler> logger) : IRequestHandler<SyncTestOrdersWithInstrumentCommand>
{
    private readonly TubeTypesSettings _settings = tubeSettings.Value;

    public async Task Handle(SyncTestOrdersWithInstrumentCommand command, CancellationToken cancellationToken)
    {
        await CancelTestOrders(command, cancellationToken);

        var request = command.Request;

        List<SampleTubeDto> sampleTubes;
        if (command.SampleTube != null)
        {
            sampleTubes = [command.SampleTube];
        }
        else
        {
            sampleTubes = await mediator.Send(new GetProcessableSampleTubesQuery(request.SampleId), cancellationToken);
        }

        if (sampleTubes.Count == 0)
        {
            // no test orders can be sent to any instrument
            return;
        }

        var tubesWithInactiveMethods = new List<SampleTubeDto>();

        var requestUpdated = false;

        foreach (var tube in sampleTubes)
        {
            var tubeType = request.GetTubeType(_settings);

            if (tubeType == null)
            {
                logger.LogError("Failed to get the tube type for a sample {SampleId}", request.SampleId);
                continue;
            }

            var volumeCheckNeeded = tubeType.VolumeCalculation && request.SampleVolumeCheck != SampleVolumeCheck.Off;

            if (volumeCheckNeeded && tube.SampleVolume == 0)
            {
                // The sample volume has not been measured or the tube is empty. No test orders can be run for this tube.
                continue;
            }

            var tubeHasStartedTests = request.TestOrders.Any(x => x.State != TestOrderState.NotStarted // we must include even completed here
                                                                && x.InstrumentNumber == tube.InstrumentNumber && x.TubeId == tube.TubeId);

            var notStartedTests = await request.GetNotStartedEnabledTests(methods);

            if (notStartedTests.Count == 0 && !tubeHasStartedTests)
            {
                // no tests to schedule or update for this tube
                continue;
            }

            if (notStartedTests.Count > 0)
            {
                var instrumentMethods = await mediator.Send(new GetActiveInstrumentMethodsQuery(tube.InstrumentNumber), cancellationToken);

                for (var i = 0; i < notStartedTests.Count;)
                {
                    // check if the test's method is active on the instrument
                    if (instrumentMethods.Contains(notStartedTests[i].MethodName))
                    {
                        i++;
                        continue;
                    }
                    notStartedTests.RemoveAt(i);
                }

                if (notStartedTests.Count == 0 && !tubeHasStartedTests)
                {
                    tubesWithInactiveMethods.Add(tube);
                    // no tests to schedule or update for this tube
                    continue;
                }
            }

            var scheduledTestOrders = request.TestOrders.Where(x => x.State is TestOrderState.Scheduled or TestOrderState.UnableToStart
                                                               && x.InstrumentNumber == tube.InstrumentNumber && x.TubeId == tube.TubeId && x.IsActive).ToList();

            // we need to send a full update if there are any modifications to scheduled tests
            // or if there are not started tests that should be run before already scheduled tests (based on TestSortOrder)
            var fullUpdateNeeded = scheduledTestOrders.Any(x => x.EntityState == EntityState.Modified)
                                   || (notStartedTests.Count > 0 && notStartedTests.Any(n => scheduledTestOrders.Any(s => n.TestSortOrder < s.TestSortOrder)));

            var testsToSend = notStartedTests;
            if (fullUpdateNeeded)
            {
                testsToSend.AddRange(scheduledTestOrders);
            }

            try
            {
                testsToSend = testsToSend.OrderBy(x => x.TestSortOrder).ToList();

                if (volumeCheckNeeded)
                {
                    testsToSend = await CheckSampleVolume(request, testsToSend, tube, cancellationToken);
                }

                if (testsToSend.Count == 0)
                {
                    // no test orders can be sent to the instrument after all checks
                    continue;
                }

                var testOrderRequest = new TestOrderRequest
                {
                    Samples =
                    {
                        new SampleTube
                        {
                            Sample = new SampleInformation
                            {
                                SampleId = request.SampleId,
                                SampleDate = request.Created.ToTimestamp(),
                                HighPriority = request.HighPriority || testsToSend.Any(x => x.IsReflexTest),
                                PreDilutionFactor = request.PreDilutionFactor,
                                TubeId = tube.TubeId,
                                TubeTypeIndex = tubeType.TubeDefinitionId,
                                IdentitySource = tube.TubeIdentitySource.ToIdentitySource()
                            },
                            TestOrders =
                            {
                                await TestOrderMapper.MapTestOrders(testsToSend, articles)
                            },
                            DoNotStartIfVolumeNotEnough = request.SampleVolumeCheck == SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow
                        }
                    },
                    CustomRunId = testsToSend.Where(t => t.TestOrderCustomParameters != null).Select(t => t.TestOrderCustomParameters!.CustomRunId).FirstOrDefault() ?? string.Empty,
                };

                var sent = await mediator.Send(new UpdateTestOrderCommand(tube.InstrumentNumber, testOrderRequest), cancellationToken).ConfigureAwait(false);

                if (sent)
                {
                    // update statues of test orders and requests
                    testsToSend.ForEach(x =>
                    {
                        if (x.State == TestOrderState.NotStarted)
                        {
                            x.State = TestOrderState.Scheduled;
                            x.TubeId = tube.TubeId;
                            x.InstrumentNumber = tube.InstrumentNumber;
                            requestUpdated = true;
                        }
                    });
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update test orders for instrument {Instrument}", tube.InstrumentNumber);
            }
        }

        if (requestUpdated)
        {
            request.UpdateRequestState();
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (command.PublishRequestUpdatedEvent)
            {
                await mediator.Publish(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()), cancellationToken);
            }
        }

        if (tubesWithInactiveMethods.Count > 0)
        {
            foreach (var instr in tubesWithInactiveMethods.GroupBy(x => x.InstrumentNumber))
            {
                await mediator.Publish(new NoActiveMethodsForTubeEvent(instr.Key, instr.Select(x => x.TubeId).ToList()), cancellationToken);
            }
        }
    }

    private async Task CancelTestOrders(SyncTestOrdersWithInstrumentCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var testOrdersToCancel = request.TestOrders
            .Where(x => !string.IsNullOrEmpty(x.InstrumentNumber)
                        && x is
                        {
                            EntityState: EntityState.Modified, IsActive: false,
                            State: TestOrderState.Scheduled or TestOrderState.UnableToStart
                        })
            .ToList(); // deactivated tests

        if (command.DeletedTestOrders is { Count: > 0 })
        {
            testOrdersToCancel.AddRange(command.DeletedTestOrders.Where(x => !string.IsNullOrEmpty(x.InstrumentNumber)).ToList());
        }

        // Group test orders by instrument number and send delete commands
        var testOrdersByInstrument = testOrdersToCancel
            .GroupBy(x => x.InstrumentNumber!)
            .ToList();

        foreach (var instrumentGroup in testOrdersByInstrument)
        {
            var deleteRequest = new DeleteTestOrderRequest();
            foreach (var testOrder in instrumentGroup)
            {
                deleteRequest.TestOrderIds.Add(testOrder.Uid.ToString());
            }

            try
            {
                await mediator.Send(new DeleteTestOrderCommand(instrumentGroup.Key, deleteRequest), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to cancel test orders for instrument {Instrument}", instrumentGroup.Key);
            }
        }
    }

    private async Task<List<Domain.TestOrder>> CheckSampleVolume(Domain.Request request, List<Domain.TestOrder> testToSend, SampleTubeDto tube, CancellationToken cancellationToken)
    {
        var result = testToSend.ToList();
        var testsToCancel = new List<Domain.TestOrder>();

        // we must always include the non-pipetted tests in the calculation of needed sample volume, because those tests will also consume sample volume,
        // even though they are not included in this batch of test orders to be sent to instrument.
        var nonPipettedTests = request.TestOrders
            .Where(x => x.TubeId == tube.TubeId && x.InstrumentNumber == tube.InstrumentNumber
                                                && x.State is TestOrderState.Scheduled or TestOrderState.UnableToStart or TestOrderState.Started
                                                && testToSend.IndexOf(x) == -1)
            .ToList();

        var combinedTests = new List<Domain.TestOrder>(nonPipettedTests);
        combinedTests.AddRange(testToSend);
        var mappedTests = await TestOrderMapper.MapTestOrders(combinedTests, articles);

        // If the sample volume is known, it means that the volume pre-measurement is done,
        // and we need to calculate the needed sample volume for the test orders.
        var response = await mediator.Send(new GetNeededSampleVolumeQuery(tube.InstrumentNumber, mappedTests), cancellationToken).ConfigureAwait(false);
        if (response == null)
        {
            // There was an error calculating the needed sample volume, we will not send any test orders to the instrument.
            return new List<Domain.TestOrder>();
        }

        // we start from the last test order because the needed sample volume is accumulated from the last test order to the first test order,
        // and we want to find out how many test orders can be run with the current sample volume.
        for (var i = response.NeededSampleVolumes.Count - 1; i >= nonPipettedTests.Count; i--)
        {
            var neededSampleVolume = response.NeededSampleVolumes[i].NeededSampleVolumeAccumulated;
            if (tube.SampleVolume < neededSampleVolume)
            {
                if (request.SampleVolumeCheck == SampleVolumeCheck.OnRunNoTestsWhenVolumeTooLow)
                {
                    // If the sample volume is too low for all test orders, we will not run any test order.
                    result.Clear();
                    testsToCancel.AddRange(testToSend.Where(x => x.State != TestOrderState.NotStarted));
                    break;
                }

                var lastIndex = result.Count - 1;
                if (result[lastIndex].State != TestOrderState.NotStarted)
                {
                    // we might need to cancel an already scheduled test order
                    // if the sample volume is too low after test order change.
                    testsToCancel.Add(result[lastIndex]);
                }
                result.RemoveAt(lastIndex);
            }
        }

        if (result.Count == 0)
        {
            // Nothing to send because the sample volume is too low
            await mediator.Publish(new NotEnoughSampleVolumeEvent(tube.InstrumentNumber, tube.TubeId), cancellationToken).ConfigureAwait(false);
        }

        if (testsToCancel.Count > 0)
        {
            var deleteRequest = new DeleteTestOrderRequest();
            testsToCancel.ForEach(x => deleteRequest.TestOrderIds.Add(x.Uid.ToString()));

            await mediator.Send(new DeleteTestOrderCommand(tube.InstrumentNumber, deleteRequest), cancellationToken);
        }

        return result;
    }
}