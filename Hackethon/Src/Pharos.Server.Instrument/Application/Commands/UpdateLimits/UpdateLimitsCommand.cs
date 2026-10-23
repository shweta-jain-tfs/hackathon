using MediatR;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument.Limits;

namespace Pharos.Server.Instrument.Application.Commands.UpdateLimits;

internal record UpdateLimitsCommand(string InstrumentNumber, Guid? SessionId) : IStateSynchronizationCommand;

internal class UpdateLimitsCommandHandler(
    ICommunicationSessionManager sessionManager,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<UpdateLimitsCommand>(sessionManager, CommunicationSynchronizationState.UpdateLimitsSent, publisher)
{
    protected override async Task DoHandle(UpdateLimitsCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        // TODO: Can this command be removed? The limits should come from the instrument itself...
        // TODO: Update with dynamic data, for now just sending defaults. 
        var limitsInfo = new Limits
        {
            BlankLimits =
            {
                new BlankLimit
                {
                    BlankType = EnumBlankType.ReagentImmunoCap,
                    UpperLimit = 1.2
                },
                new BlankLimit
                {
                    BlankType = EnumBlankType.ReagentElia,
                    UpperLimit = 1.2
                },
                new BlankLimit
                {
                    BlankType = EnumBlankType.Rinse,
                    UpperLimit = 1
                }
            },
            FluoroCLimits = new FluoroCLimit
            {
                BlankLimit = 1,
                CvLimit = 1.1,
                DeviationFirstLimit = 4,
                DeviationSecondLimit = 10
            },
            FluorometerLimits = new FluorometerLimit
            {
                FluorometerCvLimit = 1,
                FluorometerLedCurrentUpperLimit = 50,
                FluorometerLedCurrentLowerLimit = 10,
                FluorometerTemperatureUpperLimit = 0,
                FluorometerTemperatureLowerLimit = 0
            },
            PredictiveMaintenanceLimits =
            {
                new PredictiveMaintenanceLimit
                {
                    Module = EnumModule.UnspecifiedModule,
                    ModuleType = EnumModuleType.UnspecifiedModuleType,
                    MinLimit = 0,
                    MaxLimit = 0,
                    MaxTimeOutOfRange = 0,
                    Reset = false
                }
            }
        };

        await session.Connection.UpdateLimitsAsync(limitsInfo, cancellationToken);
    }
}