using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Interfaces;

namespace Pharos.Server.Instrument.Application.Queries.GetInstrumentMethods;

public class InstrumentMethodDto
{
    public int Id { get; set; }
    public string MethodName { get; set; } = default!;
    public bool IsActive { get; set; }
    public int CalibrationValidForDays { get; set; }
    public int MaximumNumberOfDays { get; set; }
    public InstrumentMethodWhenToRun WhenToRun { get; set; }
    public bool Calibrator { get; set; }
    public bool ForceCalibrator { get; set; }
    public string ConjugateLotNumber { get; set; } = string.Empty;
    public string CarrierLotNumber { get; set; } = string.Empty;
}

internal record GetInstrumentMethodsQuery(int InstrumentId) : IRequest<List<InstrumentMethodDto>>;

internal class GetInstrumentMethodsQueryHandler(IInstrumentDbContext dbContext, IMediator mediator, IInstrumentManager instrumentManager, IMethodsService methodsService) : IRequestHandler<GetInstrumentMethodsQuery, List<InstrumentMethodDto>>
{
    public async Task<List<InstrumentMethodDto>> Handle(GetInstrumentMethodsQuery request, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(request.InstrumentId);

        // Get the instrument methods.
        var instrumentMethods =
            await dbContext.InstrumentMethods
                .Where(x => x.Instrument.Id == request.InstrumentId)
                .Select(x => new InstrumentMethodDto
                {
                    Id = x.Id,
                    IsActive = x.IsActive,
                    MethodName = x.MethodName,
                    WhenToRun = x.WhenToRun,
                    Calibrator = x.Calibrator,
                    ForceCalibrator = x.ForceCalibrator,
                    ConjugateLotNumber = x.ConjugateLotNumber,
                    CarrierLotNumber = x.CarrierLotNumber
                })
                .ToListAsync(cancellationToken);

        var calibrationData = await mediator.Send(new GetCalibrationValidDaysQuery(instrument.SerialNumber), cancellationToken);
        var calibrationLookup = calibrationData.ToLookup(x => x.MethodName);

        foreach (var instrumentMethod in instrumentMethods)
        {
            // If calibrator, set CalibrationValidForDays to zero.
            if (instrumentMethod.Calibrator)
            {
                instrumentMethod.CalibrationValidForDays = 0;

                var maxDaysInfo = calibrationLookup[instrumentMethod.MethodName].FirstOrDefault();
                if (maxDaysInfo != null)
                {
                    instrumentMethod.MaximumNumberOfDays = maxDaysInfo.MaxNumberOfDays;
                }
            }
            else
            {
                // Check if this method connects ImmunoCap and Conjugate lot numbers.
                var connectLotNumbers = await ConnectImmunoCapAndConjugateLotNumbers(instrumentMethod.MethodName);

                CalibrationValidDaysDto? calibrationInfo;

                if (connectLotNumbers)
                {
                    // Look up using both conjugate lot number AND carrier lot number.
                    calibrationInfo = calibrationLookup[instrumentMethod.MethodName].FirstOrDefault(x =>
                        x.MethodName == instrumentMethod.MethodName &&
                        x.ConjugateLotNumber == instrumentMethod.ConjugateLotNumber &&
                        x.ImmunoCapLotNumber == instrumentMethod.CarrierLotNumber);
                }
                else
                {
                    // Look up using only the conjugate lot number.
                    calibrationInfo = calibrationLookup[instrumentMethod.MethodName].FirstOrDefault(x =>
                        x.MethodName == instrumentMethod.MethodName &&
                        x.ConjugateLotNumber == instrumentMethod.ConjugateLotNumber);
                }

                if (calibrationInfo != null)
                {
                    instrumentMethod.CalibrationValidForDays = calibrationInfo.CalibrationValidForDays;
                    instrumentMethod.MaximumNumberOfDays = calibrationInfo.MaxNumberOfDays;
                }
            }
        }

        var orderedMethodNames = await methodsService.GetMethods(m => true, x => x.MethodName);
        var orderByMethodName = orderedMethodNames
            .Select((name, index) => new { name, index })
            .ToDictionary(x => x.name, x => x.index);

        return instrumentMethods
            .OrderBy(x => orderByMethodName.GetValueOrDefault(x.MethodName, int.MaxValue))
            .ThenBy(x => x.MethodName)
            .ToList();
    }

    /// <summary>
    /// Check if the method has connection with lot numbers on conjugate AND ImmunoCap
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>True if conjugate and ImmunoCap lot numbers are connected</returns>
    private async Task<bool> ConnectImmunoCapAndConjugateLotNumbers(string methodName)
    {
        var result = await methodsService.GetMethod(methodName, m => m.AnalyticalRun.RunCalibrationOnCarrierLotChange);

        return result;
    }
}