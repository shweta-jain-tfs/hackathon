using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Queries.GetInstrumentMethods;
using Tetris.Protocol.Instrument.Control;
using MethodStatusInformation = Tetris.Protocol.Instrument.Control.MethodStatusInformation;

namespace Pharos.Server.Instrument.Application.Commands.UpdateMethodStatus;

internal record UpdateMethodStatusSingleCommand(string InstrumentNumber, int InstrumentMethodId) : IInstrumentCommand, IRequest;

internal class UpdateMethodStatusSingleCommandHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentManager instrumentManager,
    IInstrumentDbContext dbContext,
    ISender sender) : InstrumentCommandHandlerBase<UpdateMethodStatusSingleCommand>(sessionManager)
{
    protected override async Task Handle(UpdateMethodStatusSingleCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(request.InstrumentNumber);
        var instrumentMethods = await sender.Send(new GetInstrumentMethodsQuery(instrument.Id), cancellationToken);

        var instrumentMethod = instrumentMethods.FirstOrDefault(x => x.Id == request.InstrumentMethodId);
        if (instrumentMethod == null)
        {
            throw new InvalidOperationException($"Instrument method with ID {request.InstrumentMethodId} not found.");
        }

        var methodStatusInformationList = new List<MethodStatusInformation>
        {
            new()
            {
                MethodName = instrumentMethod.MethodName,
                CalCCProcessing = MapWhenToRunToCalCcProcessing(instrumentMethod.WhenToRun),
                ForceCalibrator = instrumentMethod.ForceCalibrator
            }
        };

        var methodsRequest = new UpdateMethodStatusRequest();
        methodsRequest.MethodStatus.AddRange(methodStatusInformationList);

        await session.Connection.UpdateMethodStatusAsync(methodsRequest, cancellationToken);

        // After successfully sending the status, reset ForceCalibrator to false
        await ResetForceCalibrator(request.InstrumentMethodId, cancellationToken);
    }

    private async Task ResetForceCalibrator(int methodId, CancellationToken cancellationToken)
    {
        var method = await dbContext.InstrumentMethods
            .FirstOrDefaultAsync(m => m.Id == methodId, cancellationToken);

        if (method != null)
        {
            method.ForceCalibrator = false;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
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
