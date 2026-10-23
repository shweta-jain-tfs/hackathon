
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Domain.Exceptions;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Localization;
using Tetris.Protocol.Common;


namespace Pharos.Server.Instrument.Application.Commands.UpdateCurrentCalibrationInformation;

public record UpdateCurrentCalibrationInformationCommand(string InstrumentNumber, CurrentCalibrationInformation? Request) : IRequest;

internal class UpdateCurrentCalibrationInformationCommandHandler(
    IInstrumentDbContext dbContext,
    IInstrumentManager instrumentManager,
    ICurrentInstrumentServerHub hub) : IRequestHandler<UpdateCurrentCalibrationInformationCommand>
{
    public async Task Handle(UpdateCurrentCalibrationInformationCommand request, CancellationToken cancellationToken)
    {
        // If Request is null (instrument not connected), nothing to update
        if (request.Request == null)
        {
            return;
        }

        var instrument = instrumentManager.FindInstrument(request.InstrumentNumber);
        if (instrument == null)
        {
            throw new ObjectGoneException(Translations.InstrumentDeleted);
        }

        // Get all instrument methods.
        var instrumentMethodsLookup = await dbContext.InstrumentMethods
            .Where(x => x.Instrument.Id == instrument.Id)
            .ToDictionaryAsync(x => x.MethodName, cancellationToken);

        foreach (var methodCalibration in request.Request.MethodCurrentCalibrationInformation)
        {
            if (instrumentMethodsLookup.TryGetValue(methodCalibration.MethodName, out var existing))
            {
                existing.Calibrator = methodCalibration.Calibrator;
                existing.CarrierLotNumber = methodCalibration.UsedLot.CarrierLotNumber;
                existing.ConjugateLotNumber = methodCalibration.UsedLot.ConjugateLotNumber;

                // If Calibrator is true, reset ForceCalibrator.
                if (methodCalibration.Calibrator)
                {
                    existing.ForceCalibrator = false;
                }
            }
            else
            {
                throw new NotFoundException(Translations.InstrumentMethodNotFound);
            }
        }

        // Save changes for all updates.
        await dbContext.SaveChangesAsync(cancellationToken);

        await hub.InstrumentMethodsChanged(instrument.Id);
    }
}