using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Result;
using Pharos.Server.Result.Application.Events;
using Pharos.Server.Result.Application.Interfaces;
using Pharos.Server.Result.Application.Services;
using Pharos.Server.Result.Domain;
using Tetris.Protocol.Common;
using ValidationException = FluentValidation.ValidationException;

namespace Pharos.Server.Result.Application.Commands;

internal sealed class ProcessBlankResultCommandHandler(
    IResultDbContext dbContext,
    IMediator mediator,
    ILogger<ProcessBlankResultCommandHandler> logger) : IRequestHandler<ProcessBlankResultCommand>
{
    public async Task Handle(ProcessBlankResultCommand request, CancellationToken cancellationToken)
    {
        // Create Blank
        var message = request.Message;
        var devSolutionBarcode = message.DevelopmentSolutionIdentity.ReagentBarcode;
        var devSolutionCellPosition = message.DevelopmentSolutionIdentity.CellPosition;
        var stopSolutionBarcode = message.StopSolutionBarcode;
        var measurementTime = message.Replicates is { Count: > 0 } ? message.Replicates.Min(r => r.TimeOfMeasurement.ToDateTimeOffset().ToLocalTime()) : DateTimeOffset.Now;

        if (request.AllowRepeatedMessage)
        {
            // check for existing blank: repeated FLR-import case
            var alreadyExists = await dbContext.Blanks
                .Where(x => x.DevSolutionBarcode == devSolutionBarcode && x.DevSolutionCellPosition == devSolutionCellPosition && x.StopSolutionBarcode == stopSolutionBarcode && x.MeasurementTime == measurementTime)
                .AnyAsync(cancellationToken);

            if (alreadyExists)
            {
                logger.LogInformation("Reagent blank already exists, skipping...");
                return;
            }
        }

        var blank = new Blank
        {
            InstrumentNumber = request.InstrumentNumber,
            BlankType = message.BlankType switch
            {
                EnumBlankType.ReagentImmunoCap => BlankType.ReagentBlankImmunoCap,
                EnumBlankType.ReagentElia => BlankType.ReagentBlankEliA,
                EnumBlankType.Rinse => BlankType.RinseBlank,
                EnumBlankType.UnspecifiedBlankType => throw new ValidationException("Unspecified blank type"),
                _ => throw new ValidationException("Unknown blank type"),
            },
            ResultOk = message.ResultOk,
            LowerLimit = message.LowerLimit,
            UpperLimit = message.UpperLimit,
            MeanVoltage = message.MeanValue,
            DevSolutionBarcode = devSolutionBarcode,
            DevSolutionCellPosition = devSolutionCellPosition,
            StopSolutionBarcode = stopSolutionBarcode,
            MeasurementTime = measurementTime,
        };

        // Add replicates
        if (request.Message.Replicates is { Count: > 0 })
        {
            blank.BlankReplicates = new List<BlankReplicate>();

            foreach (var replicate in request.Message.Replicates)
            {
                var tempBlankAssay = new BlankReplicate
                {
                    MeasurementTime = replicate.TimeOfMeasurement.ToDateTimeOffset().ToLocalTime(),
                    BlankReplicateErrors = new List<BlankReplicateError>()
                };

                foreach (var error in replicate.Errors)
                {
                    var tempBlankAssayError = new BlankReplicateError
                    {
                        ErrorCode = (int)error,
                    };
                    tempBlankAssay.BlankReplicateErrors.Add(tempBlankAssayError);
                }

                tempBlankAssay.FluorometerValue = replicate.FluorometerResult.MeasurementValue;
                blank.BlankReplicates.Add(tempBlankAssay);
            }
        }

        dbContext.Blanks.Add(blank);

        // Let the world know that a new valid reagent blank is available
        if (blank is { ResultOk: true, BlankType: BlankType.ReagentBlankImmunoCap or BlankType.ReagentBlankEliA })
        {
            blank.AddDomainEvent(new NewValidReagentBlank(blank));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await mediator.Send(SendResultToLabCommunityCommandFactory.Create(blank), cancellationToken);
    }
}