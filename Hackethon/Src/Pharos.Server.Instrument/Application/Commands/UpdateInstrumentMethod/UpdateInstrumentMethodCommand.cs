using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Commands.SendActiveMethods;
using Pharos.Server.Instrument.Application.Commands.UpdateMethodStatus;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Localization;
using Tetris.Protocol.Pharos;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Instrument.Application.Commands.UpdateInstrumentMethod;

public record UpdateInstrumentMethodCommand(int InstrumentMethodId, int InstrumentId, bool IsActive, bool ForceCalibrator, InstrumentMethodWhenToRun WhenToRun) : IRequest;

internal class UpdateInstrumentMethodCommandHandler(
    IInstrumentDbContext dbContext,
    ISender sender,
    IPublisher publisher,
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub hub,
    ICommunicationSessionManager sessionManager) : IRequestHandler<UpdateInstrumentMethodCommand>
{
    public async Task Handle(UpdateInstrumentMethodCommand request, CancellationToken cancellationToken)
    {
        var instrumentMethodToUpdate = await dbContext.InstrumentMethods
            .FirstOrDefaultAsync(x => x.Id == request.InstrumentMethodId
                                      && x.Instrument.Id == request.InstrumentId, cancellationToken);

        if (instrumentMethodToUpdate == null)
        {
            throw new NotFoundException(Translations.InstrumentMethodNotFound);
        }

        var isActiveChanged = instrumentMethodToUpdate.IsActive != request.IsActive;
        var isForceCalibratorChanged = instrumentMethodToUpdate.ForceCalibrator != request.ForceCalibrator;
        var isWhenToRunChanged = instrumentMethodToUpdate.WhenToRun != request.WhenToRun;

        if (isActiveChanged || isForceCalibratorChanged || isWhenToRunChanged)
        {
            // Update instrument method with user-editable properties only
            instrumentMethodToUpdate.IsActive = request.IsActive;
            instrumentMethodToUpdate.ForceCalibrator = request.ForceCalibrator;
            instrumentMethodToUpdate.WhenToRun = request.WhenToRun;
            // Calibrator is not updated here - it's set by the instrument via CurrentCalibrationInformation

            await dbContext.SaveChangesAsync(cancellationToken);

            await hub.InstrumentMethodsChanged(request.InstrumentId);

            if (isActiveChanged)
            {
                var instrument = instrumentManager.GetInstrument(request.InstrumentId);

                // Only try to communicate with the instrument if there is an active session and connection
                var session = sessionManager.GetSession(instrument.SerialNumber);
                if (session is { CompatibilityStatus: EnumCompatibilityStatus.FullyCompatible })
                {
                    if (instrumentMethodToUpdate.IsActive &&
                        instrument.State is
                            InstrumentState.Assay or
                            InstrumentState.AssayFinishOrdered or
                            InstrumentState.BlankMeasurement or
                            InstrumentState.Priming
                       )
                    {
                        // The method is activated - send method status to instrument.
                        await sender.Send(new UpdateMethodStatusSingleCommand(instrument.SerialNumber, request.InstrumentMethodId), cancellationToken);
                    }

                    // Send active methods to instrument. Should be done after updating method status.
                    await sender.Send(new SendActiveMethodsCommand(instrument.SerialNumber), cancellationToken);

                    await publisher.Publish(new MethodActivatedEvent(instrument.SerialNumber, instrumentMethodToUpdate.MethodName, instrumentMethodToUpdate.IsActive), cancellationToken);
                }
            }
        }
    }
}