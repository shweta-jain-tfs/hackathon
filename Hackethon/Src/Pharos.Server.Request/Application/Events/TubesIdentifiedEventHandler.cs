using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Request;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Commands.TestOrderScheduling;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;

namespace Pharos.Server.Request.Application.Events;

internal class TubesIdentifiedEventHandler(
    IRequestDbContext dbContext,
    IOptionsSnapshot<RequestSettings> requestSettings,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    IDateTime dateTime,
    IMediator mediator,
    ILogger<TubesIdentifiedEventHandler> logger) : INotificationHandler<TubesIdentifiedEvent>

{
    private readonly RequestSettings _requestSettings = requestSettings.Value;
    private readonly TubeTypesSettings _tubeSettings = tubeSettings.Value;

    public async Task Handle(TubesIdentifiedEvent notification, CancellationToken cancellationToken)
    {
        // get existing requests for the newly identified tubes
        var sampleIds = notification.Tubes.Select(x => x.Tube.SampleId).Distinct().ToHashSet(); // one sample can have multiple tubes

        var query = dbContext.Requests
            .Where(x => sampleIds.Contains(x.SampleId));

        if (_requestSettings.ReuseSampleId)
        {
            var daysBeforeReuse = _requestSettings.DaysBeforeReuseOfSampleId;

            var date = dateTime.Now.Subtract(TimeSpan.FromDays(daysBeforeReuse));
            query = query.Where(x => x.Created >= date);
        }

        query = query
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.ReflexDefinitions)
            .Include(x => x.TestOrders)
            .ThenInclude(x => x.TestOrderCustomParameters);

        var requests = await query.ToDictionaryAsync(x => x.SampleId, cancellationToken).ConfigureAwait(false);
        var samplesWithoutTests = new List<string>();
        var requestsToRefresh = new List<string>();

        // process all identified tubes
        foreach (var tubeInformation in notification.Tubes)
        {
            if (!requests.TryGetValue(tubeInformation.Tube.SampleId, out var request) || request.TestOrders.Count == 0)
            {
                // no request yet or an occasional empty request (could be manually created)
                samplesWithoutTests.Add(tubeInformation.Tube.SampleId);
                continue;
            }

            requestsToRefresh.Add(tubeInformation.Tube.SampleId);

            // check if volume pre-measurement is needed and send request, if necessary
            var tubeType = request.GetTubeType(_tubeSettings);

            if (tubeType == null)
            {
                logger.LogError("Failed to get the tube type for a sample {SampleId}", request.SampleId);
                continue;
            }

            if (tubeType.VolumeCalculation && request.SampleVolumeCheck != SampleVolumeCheck.Off)
            {
                await mediator.Send(new MeasureSampleVolumeCommand(notification.InstrumentNumber, tubeInformation.Tube.TubeId, tubeType.TubeDefinitionId), cancellationToken);

                // we will send test orders only after we measure the volume
                continue;
            }

            await mediator.Send(new SyncTestOrdersWithInstrumentCommand(request, SampleTube: new SampleTubeDto(tubeInformation.Tube.TubeId, notification.InstrumentNumber, 0, tubeInformation.IdentitySource)), cancellationToken);
        }

        if (samplesWithoutTests.Count > 0)
        {
            await mediator.Send(new AskForMissingTestOrdersCommand(samplesWithoutTests, false), cancellationToken);
        }

        if (requestsToRefresh.Count > 0)
        {
            await mediator.Send(new RefreshExistingRequestsCommand(requestsToRefresh, false), cancellationToken);
        }
    }
}