using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Parameters.Methods;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Mapping;
using Tetris.Protocol.Instrument.Methods;

namespace Pharos.Server.Instrument.Application.Commands.UpdateMethods;

internal record UpdateMethodsCommand(string InstrumentNumber, Guid? SessionId = null) : IStateSynchronizationCommand;

internal class UpdateMethodsCommandHandler(
    ICommunicationSessionManager sessionManager,
    IMethodsService methodsService,
    IInstrumentDbContext dbContext,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<UpdateMethodsCommand>(sessionManager, CommunicationSynchronizationState.UpdateMethodsSent, publisher)
{
    protected override async Task DoHandle(UpdateMethodsCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var configuredMethodNames = await dbContext.InstrumentMethods
            .Where(x => x.Instrument.SerialNumber == request.InstrumentNumber)
            .Select(x => x.MethodName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var configuredMethodNameSet = configuredMethodNames.ToHashSet();
        var paramMethods = await methodsService.GetDetailedMethods();
        var instrMethods = new Methods();

        foreach (var paramMethod in paramMethods.Where(x => configuredMethodNameSet.Contains(x.MethodName)))
        {
            // sort calibrators and curve controls by expected concentration to ensure that points are in the correct order
            paramMethod.CalibrationCurve.CalibrationPoints = paramMethod.CalibrationCurve.CalibrationPoints.OrderBy(x => x.ExpectedConcentration).ToList();
            paramMethod.CurveControls = paramMethod.CurveControls.OrderBy(x => x.ExpectedConcentration).ToList();

            var instrMethod = paramMethod.ToInstrumentMethod();
            instrMethod.ConjugateName = paramMethod.Reagents.FirstOrDefault(x => x.ReagentType == ReagentType.Conjugate)?.ReagentIdentity ?? "";
            instrMethod.WashConcentrateName = paramMethod.Reagents.FirstOrDefault(x => x.ReagentType == ReagentType.WashSolutionConcentrate)?.ReagentIdentity ?? "";
            instrMethod.WashAdditiveName = paramMethod.Reagents.FirstOrDefault(x => x.ReagentType == ReagentType.WashAdditive)?.ReagentIdentity ?? "";

            instrMethods.Methods_.Add(instrMethod);
        }

        await session.Connection.UpdateMethodsAsync(instrMethods, cancellationToken);
    }
}