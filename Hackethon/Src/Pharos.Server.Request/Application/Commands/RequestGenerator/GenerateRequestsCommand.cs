using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain.Entities;
using Pharos.Server.Infrastructure;
using Pharos.Server.Request.Application.Helpers;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Application.Queries.RequestGenerator;
using Pharos.Server.Request.Domain;
using RequestCreatedEvent = Pharos.Server.Request.Application.Events.RequestCreatedEvent;

namespace Pharos.Server.Request.Application.Commands.RequestGenerator;

internal sealed record GenerateRequestsCommand(GenerateRequestsDto GenerateRequestsDto) : IRequest;

internal sealed class GenerateRequestsCommandHandler(
    IRequestDbContext dbContext,
    IMediator mediator,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    IDateTime dateTime,
    ILogger<GenerateRequestsCommandHandler> logger) : IRequestHandler<GenerateRequestsCommand>
{
    public async Task Handle(GenerateRequestsCommand request, CancellationToken cancellationToken)
    {
        var sampleIds = RequestGenerationHelper.GenerateSampleIds(request.GenerateRequestsDto);
        var trayPlacements = request.GenerateRequestsDto.PlaceSamplesInTrays
            ? RequestGenerationHelper.GenerateTrayPlacements(request.GenerateRequestsDto)
            : [];
        var defaultTubeTypeId = GetDefaultTubeTypeId(tubeSettings);
        var successfullyProcessedSampleIndexes = new List<int>();

        for (var i = 0; i < sampleIds.Count; i++)
        {
            var sampleId = sampleIds[i];
            try
            {
                var requestIdResponse = await mediator.Send(new GetRequestIdQuery(sampleId), cancellationToken);

                Domain.Request requestEntity;

                if (requestIdResponse.RequestExists)
                {
                    logger.LogError(
                        "Skipping existing sample {SampleId} because Request Generator does not update existing requests.",
                        sampleId);
                    continue;
                }

                requestEntity = new Domain.Request
                {
                    SampleId = sampleId,
                    PreDilutionFactor = request.GenerateRequestsDto.PreDilutionFactor,
                    SampleCollectionDate = dateTime.Now,
                    RequestDate = dateTime.Now,
                    SampleTubeType = defaultTubeTypeId,
                    Source = RequestSource.Generator,
                    TestOrders = new List<TestOrder>()
                };

                var markedAsHighPriority = await mediator.Send(
                    new HasSampleTubeHighPriorityQuery(new RequestUid { SampleId = requestEntity.SampleId }),
                    cancellationToken);
                if (markedAsHighPriority)
                {
                    requestEntity.HighPriority = true;
                }

                await dbContext.Requests.AddAsync(requestEntity, cancellationToken);

                var sortOrder = requestEntity.TestOrders.Count + 1;
                foreach (var test in request.GenerateRequestsDto.Tests)
                {
                    var testOrder = new TestOrder
                    {
                        Uid = Guid.NewGuid(),
                        State = TestOrderState.NotStarted,
                        MethodName = test.MethodName,
                        TestName = test.TestName,
                        LabTestName = test.TestName,
                        NumberOfReplicates = test.Replicates,
                        InstrumentDilutionFactor = test.InstrumentDilutionFactor,
                        TestSortOrder = sortOrder++
                    };

                    testOrder.ReflexDefinitions = await ReflexDefinitionHelper.CreateFreshReflexDefinitionsAsync(
                        mediator,
                        testOrder.TestName,
                        testOrder.MethodName,
                        null,
                        cancellationToken);

                    requestEntity.TestOrders.Add(testOrder);
                }

                requestEntity.UpdateRequestState();
                await dbContext.SaveChangesAsync(cancellationToken);
                await mediator.Publish(new RequestCreatedEvent(requestEntity), cancellationToken);
                successfullyProcessedSampleIndexes.Add(i);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                dbContext.ClearChanges();
                logger.LogError(
                    ex,
                    "Failed to process generated request for sample {SampleId}. Continuing with remaining samples.",
                    sampleId);
            }
        }
        if (request.GenerateRequestsDto.PlaceSamplesInTrays)
        {
            await PlaceSamplesInTrays(
            successfullyProcessedSampleIndexes,
            trayPlacements,
            sampleIds,
            cancellationToken);
        }

        //Note: Not started tests are not sent to instruments, since the tubes does not exist at this point
    }

    private async Task PlaceSamplesInTrays(
        List<int> successfullyProcessedSampleIndexes,
        List<RequestGenerationTrayPlacement> trayPlacements,
        List<string> sampleIds,
        CancellationToken cancellationToken)
    {
        foreach (var sampleIndex in successfullyProcessedSampleIndexes)
        {
            var trayPlacement = trayPlacements[sampleIndex];
            try
            {
                await mediator.Send(new SetSampleInSampleTrayPositionCommand
                {
                    SampleId = sampleIds[sampleIndex],
                    TrayId = trayPlacement.TrayId,
                    TubePosition = trayPlacement.TubePosition
                }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to place generated sample {SampleId} in tray {TrayId} position {TubePosition}. Continuing with remaining samples.",
                    sampleIds[sampleIndex],
                    trayPlacement.TrayId,
                    trayPlacement.TubePosition);
            }
        }
    }

    private static int? GetDefaultTubeTypeId(IOptionsSnapshot<TubeTypesSettings> tubeSettings)
    {
        var tubeTypes = tubeSettings.Value.TubeTypeList;
        if (int.TryParse(tubeSettings.Value.NormalTubeDefinitionId, out var normalTubeId))
        {
            var match = tubeTypes.FirstOrDefault(t => t.TubeDefinitionId == normalTubeId);
            if (match != null)
            {
                return match.TubeDefinitionId;
            }
        }

        return null;
    }
}
