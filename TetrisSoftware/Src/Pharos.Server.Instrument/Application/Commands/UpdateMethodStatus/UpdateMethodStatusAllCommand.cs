using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentMethods;
using Tetris.Protocol.Instrument.Control;
using MethodStatusInformation = Tetris.Protocol.Instrument.Control.MethodStatusInformation;

namespace Pharos.Server.Instrument.Application.Commands.UpdateMethodStatus;

internal record UpdateMethodStatusAllCommand(string InstrumentNumber, Guid? SessionId = null) : IStateSynchronizationCommand;

internal class UpdateMethodStatusAllCommandHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentManager instrumentManager,
    IInstrumentDbContext dbContext,
    ISender sender,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<UpdateMethodStatusAllCommand>(sessionManager, CommunicationSynchronizationState.UpdateMethodStatusSent, publisher)
{
    protected override async Task DoHandle(UpdateMethodStatusAllCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(request.InstrumentNumber);
        var instrumentMethods = await sender.Send(new GetInstrumentMethodsQuery(instrument.Id), cancellationToken);

        var methodStatusInformationList = instrumentMethods.Where(x => x.IsActive)
            .Select(instrumentMethod => new MethodStatusInformation
            {
                MethodName = instrumentMethod.MethodName,
                CalCCProcessing = MapWhenToRunToCalCcProcessing(instrumentMethod.WhenToRun),
                ForceCalibrator = instrumentMethod.ForceCalibrator
            })
            .ToList();

        var methodsRequest = new UpdateMethodStatusRequest();
        methodsRequest.MethodStatus.AddRange(methodStatusInformationList);

        await session.Connection.UpdateMethodStatusAsync(methodsRequest, cancellationToken);

        // After successfully sending the status, reset ForceCalibrator to false for ALL methods (active and inactive)
        var allMethodIds = instrumentMethods.Select(x => x.Id).ToList();
        if (allMethodIds.Count > 0)
        {
            await ResetForceCalibrator(allMethodIds, cancellationToken);
        }
    }

    private async Task ResetForceCalibrator(List<int> methodIds, CancellationToken cancellationToken)
    {
        var methods = await dbContext.InstrumentMethods
            .Where(m => methodIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        foreach (var method in methods)
        {
            method.ForceCalibrator = false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static EnumCalCCProcessing MapWhenToRunToCalCcProcessing(InstrumentMethodWhenToRun whenToRun)
    {
        return whenToRun switch
        {
            InstrumentMethodWhenToRun.Directly => EnumCalCCProcessing.StartCalCcprocessingAssayState,
            InstrumentMethodWhenToRun.AtFirstTest => EnumCalCCProcessing.StartCalCcprocessingFirstSampleScheduled,
            _ => throw new ArgumentOutOfRangeException(nameof(whenToRun), whenToRun, null)
        };
    }
}
