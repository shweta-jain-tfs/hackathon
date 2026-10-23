using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Request;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Request.Application.Interfaces;
using Pharos.Server.Request.Domain;
using Tetris.Protocol.Pharos.TestResult;
using EntityState = Pharos.Server.Application.Request.EntityState;
using RequestCreatedEvent = Pharos.Server.Request.Application.Events.RequestCreatedEvent;

namespace Pharos.Server.Request.Application.Commands;

internal class PreProcessAssayResultCommand(
    IRequestDbContext dbContext,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    ITestDisplayNameProvider displayNameProvider,
    ISender sender) : IRequestPreProcessor<ProcessAssayResultCommand>
{
    public async Task Process(ProcessAssayResultCommand command, CancellationToken cancellationToken)
    {
        var message = command.Message;
        if (message.TestType != EnumTestType.Sample)
        {
            return;
        }

        var testOrderUid = Guid.Parse(message.Replicate.GroupId);
        var existingTestOrder = await dbContext.TestOrders.FirstOrDefaultAsync(x => x.Uid == testOrderUid, cancellationToken).ConfigureAwait(false);

        var sampleId = message.Sample.SampleId;
        var sampleDate = message.Sample.SampleDate?.ToDateTimeOffset().ToLocalTime() ?? DateTimeOffset.MinValue;

        var baseQuery = dbContext.Requests
                .Include(x => x.TestOrders)
                .ThenInclude(x => x.ReflexDefinitions);

        Domain.Request? request;
        if (existingTestOrder is not null)
        {
            request = await baseQuery
                .FirstOrDefaultAsync(x => x.Id == existingTestOrder.RequestId, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var requestId = await sender.Send(new GetRequestIdQuery(sampleId, sampleDate), cancellationToken).ConfigureAwait(false);
            request = requestId.RequestExists
                ? await baseQuery.FirstOrDefaultAsync(x => x.Id == requestId.RequestId, cancellationToken).ConfigureAwait(false)
                : null;
        }

        var newRequest = request is null;
        if (request is null)
        {
            // Create a new request if it does not exist
            request = new Domain.Request
            {
                SampleId = sampleId,
                PreDilutionFactor = message.Sample.PreDilutionFactor,
                Created = sampleDate,
                HighPriority = message.Sample.HighPriority,
                SampleVolumeCheck = SampleVolumeCheck.Off,
                Source = RequestSource.Manual
            };

            // Check with sample module if any tubes are marked as high priority
            var markedAsHighPriority = await sender.Send(new HasSampleTubeHighPriorityQuery(request.GetRequestUid()), cancellationToken).ConfigureAwait(false);
            if (markedAsHighPriority)
            {
                request.HighPriority = markedAsHighPriority;
            }

            // Set the request's sample tube type based on the settings
            if (int.TryParse(tubeSettings.Value.NormalTubeDefinitionId, out var normalTubeId))
            {
                request.SampleTubeType = tubeSettings.Value.TubeTypeList
                    .FirstOrDefault(t => t.TubeDefinitionId == normalTubeId)?.TubeDefinitionId;
            }

            request.AddDomainEvent(new RequestCreatedEvent(request));
            dbContext.Requests.Add(request);
        }

        if (existingTestOrder is null)
        {
            var testOrder = new TestOrder
            {
                Uid = Guid.Parse(message.Replicate.GroupId),
                Request = request,
                MethodName = message.MethodName,
                TestName = message.TestName,
                LabTestName = await displayNameProvider.GetTestDisplayName(message.MethodName, message.TestName).ConfigureAwait(false),
                InstrumentDilutionFactor = message.InstrumentDilutionFactor,
                NumberOfReplicates = message.Replicate.ReplicatesInTestOrder,
                InstrumentNumber = command.InstrumentNumber,
                TubeId = command.Message.Sample.TubeId,
                State = TestOrderState.Pipetted,
                EntityState = EntityState.Added,
                TestSortOrder = request.TestOrders.Any() ? request.TestOrders.Max(x => x.TestSortOrder) + 1 : 1
            };

            await dbContext.TestOrders.AddAsync(testOrder, cancellationToken).ConfigureAwait(false);
            if (!newRequest)
            {
                testOrder.AddDomainEvent(new RequestUpdatedEvent(request.Id, request.GetRequestUid(), request.TestOrders.ToList()));
            }
        }
        else
        {
            existingTestOrder.State = TestOrderState.Pipetted;
        }

        request.UpdateRequestState();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
