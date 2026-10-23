using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Result;
using Pharos.Server.Domain.Enums;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.TestResult;

namespace Pharos.Server.Result.Application.Commands.ProcessAssayResult;

internal class ProcessAssayResultCommandHandler(
    ISender sender,
    IMethodsService methodsService,
    ILogger<ProcessAssayResultCommandHandler> logger) : IRequestHandler<ProcessAssayResultCommand>
{
    public async Task Handle(ProcessAssayResultCommand request, CancellationToken cancellationToken)
    {
        request.Message.TestType = await CheckTestType(request.Message);

        BaseProcessAssayResultCommand command = request.Message.TestType switch
        {
            EnumTestType.Sample => new ProcessSampleResultCommand(),
            EnumTestType.QualityControl => new ProcessQualityControlResultCommand(),
            EnumTestType.CurveControl => new ProcessCurveControlResultCommand(),
            EnumTestType.Calibrator => new ProcessCalibratorResultCommand(),
            _ => throw new ArgumentException("Unknown test type.")
        };

        command.InstrumentNumber = request.InstrumentNumber;
        command.Message = request.Message;
        command.AllowRepeatedMessage = request.AllowRepeatedMessage;

        await sender.Send(command, cancellationToken);
    }

    /// <summary>
    /// Check test type:
    /// - If it is a Curve Control and the method is using the ViewAllergy technology, then treat it as a Calibrator
    /// </summary>
    /// <param name="assayResult">Result message</param>
    /// <returns>Test type to use</returns>
    private async Task<EnumTestType> CheckTestType(AssayResult assayResult)
    {
        var testType = assayResult.TestType;
        var methodName = assayResult.MethodName;

        var actualTestType = testType;

        if (testType == EnumTestType.CurveControl)
        {
            if (await IsViewAllergyMethod(methodName))
            {
                actualTestType = EnumTestType.Calibrator;
                logger.LogInformation("ViewAllergy: Received result message for CC, changed to Calibrators");

                foreach (var reagentInformation in assayResult.Reagents)
                {
                    if (reagentInformation.ReagentType == EnumReagentType.CurveControlCartridge)
                    {
                        reagentInformation.ReagentType = EnumReagentType.CalibratorCartridge;
                    }
                }
            }
        }

        return actualTestType;
    }

    /// <summary>
    /// Check if given method is using the ViewAllergy technology
    /// </summary>
    /// <param name="methodName">Method name</param>
    /// <returns>True if given method is using the ViewAllergy technology</returns>
    private async Task<bool> IsViewAllergyMethod(string methodName)
    {
        return await methodsService.GetMethod(methodName, m => m.Technology) == AssayTechnology.ViewAllergy;
    }
}