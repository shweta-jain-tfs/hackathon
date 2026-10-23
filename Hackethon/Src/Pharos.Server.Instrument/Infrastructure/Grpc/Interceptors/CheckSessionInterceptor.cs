using Grpc.Core;
using Grpc.Core.Interceptors;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Application.Services;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Instrument.Infrastructure.Grpc.Interceptors;

internal class CheckSessionInterceptor(ICommunicationSessionManager sessionManager) : Interceptor
{
    private readonly string[] _fileApiMethods =
    [
        nameof(PharosService.PharosServiceBase.SendUsbEvent),
        nameof(PharosService.PharosServiceBase.SendSoftwareUpdateStatus),
        nameof(PharosService.PharosServiceBase.SendPrepareLogsResult)
    ];

    private readonly string[] _allowedSessionSynchronizationMethods = [
        nameof(PharosService.PharosServiceBase.SendModuleInformation),
        nameof(PharosService.PharosServiceBase.SendInstrumentState),
        nameof(PharosService.PharosServiceBase.SendProcessStatus),
        nameof(PharosService.PharosServiceBase.SendSensorInformation),
        nameof(PharosService.PharosServiceBase.SendInstrumentStorage),
        nameof(PharosService.PharosServiceBase.SendCurrentErrorList),
        nameof(PharosService.PharosServiceBase.SendSignalTowerState),
        nameof(PharosService.PharosServiceBase.SendCurrentMaintenanceInformation),
        nameof(PharosService.PharosServiceBase.SendLasStatus),
        nameof(PharosService.PharosServiceBase.SendMissingConsumables)
    ];

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var method = context.Method;
        var isStartSessionCall = method.EndsWith(nameof(PharosService.PharosServiceBase.StartSession));
        if (isStartSessionCall)
        {
            return await continuation(request, context).ConfigureAwait(false);
        }

        var sessionId = context.GetSessionId();
        var session = sessionManager.GetSession(sessionId);
        if (session == null)
        {
            throw new RpcException(new Status(StatusCode.DataLoss, $"Unknown session identifier: {sessionId}. Restart the session by calling {nameof(PharosServiceImplementation.StartSession)}."));
        }

        if (session.CompatibilityStatus == EnumCompatibilityStatus.FullyCompatible)
        {
            if (!(session.IsSynchronized || _allowedSessionSynchronizationMethods.Any(method.EndsWith)))
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"{method} call is not allowed before the communication session is fully synchronized."));
            }
        }
        else if (session.CompatibilityStatus == EnumCompatibilityStatus.FilesApiOnly)
        {
            if (!_fileApiMethods.Any(method.EndsWith))
            {
                throw new RpcException(new Status(StatusCode.PermissionDenied, $"{method} call is not allowed for a session with FilesApiOnly compatibility."));
            }
        }
        else
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, $"{method} call is not allowed because Pharos and ISW are incompatible."));
        }

        return await continuation(request, context).ConfigureAwait(false);
    }
}