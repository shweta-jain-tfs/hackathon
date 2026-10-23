using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.LoadList;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Application.Reagents;
using Pharos.Server.Application.Result;
using Pharos.Server.Application.Sample;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.ProcessInstrumentState;
using Pharos.Server.Instrument.Application.Commands.ProcessModuleInformation;
using Pharos.Server.Instrument.Application.Commands.ProcessSensorInformation;
using Pharos.Server.Instrument.Application.Commands.StartSession;
using Pharos.Server.Instrument.Application.Commands.UpdateCurrentCalibrationInformation;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Pharos.Server.Instrument.Infrastructure.Grpc;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos;
using Tetris.Protocol.Pharos.Consumables;
using Tetris.Protocol.Pharos.Errors;
using Tetris.Protocol.Pharos.Files;
using Tetris.Protocol.Pharos.Move;
using Tetris.Protocol.Pharos.ProcessStatus;
using Tetris.Protocol.Pharos.Storage;
using Tetris.Protocol.Pharos.TestResult;
using Tetris.Protocol.Pharos.Tray;

namespace Pharos.Server.Instrument.Application.Services;

internal class PharosServiceImplementation(
    ICommunicationSessionManager sessionManager,
    IMediator mediator,
    IPharosVersionService pharosVersion,
    ITetrisProtocolInformation protocolInformation,
    IMethodsService methodsService,
    IValidator<InstrumentStorage> instrumentStorageValidator,
    IValidator<TestOrderStatusChange> testOrderStatusChangeValidator,
    ILogger<PharosServiceImplementation> logger) : PharosService.PharosServiceBase
{
    public override async Task<ReagentDrawerContentsResponse> GetReagentDrawerContents(ReagentDrawerContentsRequest request, ServerCallContext context)
    {
        return await mediator.Send(new GetReagentDrawerContentsCommand(context.GetInstrumentSerial(), request));
    }

    public override async Task<SampleDrawerContentsResponse> GetSampleDrawerContents(SampleDrawerContentsRequest request, ServerCallContext context)
    {
        return await mediator.Send(new GetSampleDrawerContentsCommand(context.GetInstrumentSerial(), request));
    }

    public override async Task<Empty> SendSensorInformation(SensorInformation request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Send(new ProcessSensorInformationCommand(instrumentNumber, request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.SensorInformationReceived);
    }

    public override async Task<Empty> SendAnalyticalRunEnd(AnalyticalRunEnd request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Send(new EndAnalyticalRunCommand(instrumentNumber, request.MethodName, request.AnalyticalRunId, request.AssayRunId));
        return new Empty();
    }

    public override async Task<Empty> SendAssayRunEnd(AssayRunEnd request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Send(new EndAssayRunCommand(instrumentNumber, request.AssayRunId, request.CustomRunId));
        return new Empty();
    }

    public override async Task<Empty> SendAssayResult(AssayResult request, ServerCallContext context)
    {
        var serialNumber = context.GetInstrumentSerial();

        var doesMethodAndTestExist = await methodsService.GetMethodTest(request.MethodName, request.TestName, x => true);
        if (doesMethodAndTestExist)
        {
            await mediator.Send(new ProcessAssayResultCommand(serialNumber, request));
        }
        else
        {
            await mediator.Send(new CreatePharosErrorCommand(PharosErrorCodes.ResultForUnknownMethodAndTestCombination, ErrorClassification.Warning, serialNumber, [request.MethodName, request.TestName]));
            logger.LogError("Received assay result for unknown method and test combination from {InstrumentSerial}. Method: {MethodName}, Test: {TestName}", serialNumber, request.MethodName, request.TestName);
        }

        return new Empty();
    }

    public override async Task<Empty> SendBlankResult(BlankResult request, ServerCallContext context)
    {
        await mediator.Send(new ProcessBlankResultCommand(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<Empty> SendCurrentErrorList(CurrentErrorList request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Publish(new InstrumentErrorListUpdatedEvent(instrumentNumber, request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.CurrentErrorListReceived);
    }

    public override async Task<Empty> SendCurrentCalibrationInformation(CurrentCalibrationInformation request, ServerCallContext context)
    {
        await mediator.Send(new UpdateCurrentCalibrationInformationCommand(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<Empty> SendMissingConsumables(MissingConsumablesInfo request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Send(new MissingConsumablesUpdatedCommand(instrumentNumber, request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.MissingConsumablesReceived);
    }

    public override async Task<Empty> SendInstrumentState(Tetris.Protocol.Pharos.InstrumentState request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        var sessionId = context.GetSessionId();
        await mediator.Send(new ProcessInstrumentStateCommand(instrumentNumber, sessionId, request));
        return new Empty();
    }

    public override async Task<Empty> SendInstrumentStorage(InstrumentStorage request, ServerCallContext context)
    {
        await instrumentStorageValidator.ValidateAndThrowAsync(request);

        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Publish(new InstrumentStorageUpdatedEvent(instrumentNumber, request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.InstrumentStorageReceived);
    }

    public override async Task<Empty> SendLoadReagentResult(LoadReagentResult request, ServerCallContext context)
    {
        await mediator.Send(new ProcessLoadReagentResultCommand(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<Empty> SendModuleInformation(ModuleInformation request, ServerCallContext context)
    {
        await mediator.Send(new ProcessModuleInformationCommand(context.GetInstrumentIp(), context.GetSessionId(), request));
        return new Empty();
    }

    public override async Task<Empty> SendProcessStatus(ProcessStatus request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Publish(new ProcessStatusUpdatedEvent(instrumentNumber, request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.ProcessStatusReceived);
    }

    public override async Task<ScannedSampleQcResponse> SendScannedSampleQc(ScannedSampleQcRequest request, ServerCallContext context)
    {
        return await mediator.Send(new ProcessScannedTubeCommand(context.GetInstrumentSerial(), request));
    }

    public override async Task<Empty> SendTestOrderStatusChange(TestOrderStatusChange request, ServerCallContext context)
    {
        await testOrderStatusChangeValidator.ValidateAndThrowAsync(request);
        // this is a critical message that must be processed immediately, not in the background, so we use Send, not Publish
        await mediator.Send(new TestOrderStatusChangedEvent(request));
        return new Empty();
    }

    public override async Task<Empty> SendUsbEvent(UsbEvent request, ServerCallContext context)
    {
        await mediator.Publish(new InstrumentUsbEvent(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<Empty> SendNfcEvent(NfcEvent request, ServerCallContext context)
    {
        if (request.Type == EnumNfcEventType.Scanned)
        {
            await mediator.Publish(new NfcCardScannedEvent(context.GetInstrumentSerial(), request.ChipUid));
        }
        else if (request.Type == EnumNfcEventType.NfcEventError)
        {
            await mediator.Publish(new NfcCardErrorEvent(context.GetInstrumentSerial()));
        }
        else
        {
            throw new ValidationException("Unknown NFC event type.");
        }

        return new Empty();
    }

    public override async Task<Empty> SendScannedBarcode(ScannedBarcodeRequest request, ServerCallContext context)
    {
        await mediator.Publish(new ScannedBarcodeEvent(context.GetInstrumentSerial(), request.Barcode));
        return new Empty();
    }

    public override async Task<SessionResponse> StartSession(SessionRequest request, ServerCallContext context)
    {
        var compatibilityStatus = await mediator.Send(new StartSessionCommand(
            context.GetInstrumentIp(),
            context.GetSessionId(),
            context.GetInstrumentSerial(),
            request.ProtocolVersion,
            request.InstrumentSoftwareVersion,
            request.InstrumentServicePort));

        return new SessionResponse
        {
            PharosSoftwareVersion = pharosVersion.Version,
            ProtocolVersion = protocolInformation.VersionString,
            CompatibilityStatus = compatibilityStatus
        };
    }

    public override async Task<Empty> SendUnlockButton(UnlockButtonRequest request, ServerCallContext context)
    {
        await mediator.Publish(new UnlockDrawerAuthorizationEvent(context.GetInstrumentSerial(), request.Compartment));
        return new Empty();
    }

    public override async Task<Empty> SendSignalTowerState(SignalTowerState request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Publish(new SignalTowerStateChangedEvent(instrumentNumber, request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.SignalTowerUpdated);
    }

    public override async Task<SampleQcDestinationResponse> GetSampleQcDestination(SampleQcDestinationRequest request, ServerCallContext context)
    {
        return await mediator.Send(new GetSampleQcDestinationQuery(context.GetInstrumentSerial(), request));
    }

    public override async Task<ScannedCarrierResponse> SendScannedCarrier(ScannedCarrierRequest request, ServerCallContext context)
    {
        return await mediator.Send(new SendScannedCarrierCommand(context.GetInstrumentSerial(), request));
    }

    public override async Task<Empty> SendEvent(Event request, ServerCallContext context)
    {
        await mediator.Publish(new InstrumentEventReceived(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<Empty> SendMoveSampleQcResult(MoveSampleQcResult request, ServerCallContext context)
    {
        await mediator.Send(new MoveSampleQcResultCommand(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<MoveSampleQcResponse> GetMoveSampleQcRequest(Empty request, ServerCallContext context)
    {
        //TODO: Placeholder for future implementation
        return new MoveSampleQcResponse();
    }

    public override async Task<Empty> SendUnloadReagentResult(UnloadReagentResult request, ServerCallContext context)
    {
        await mediator.Send(new ProcessUnloadReagentResultCommand(context.GetInstrumentSerial(), request));
        return new Empty();
    }

    public override async Task<Empty> SendCurrentMaintenanceInformation(CurrentMaintenanceInformation request, ServerCallContext context)
    {
        var instrumentNumber = context.GetInstrumentSerial();
        await mediator.Publish(new CurrentMaintenanceInformationReceivedEvent(context.GetInstrumentSerial(), request));
        return await UpdateSyncState(instrumentNumber, CommunicationSynchronizationState.CurrentMaintenanceInfoReceived);
    }

    private async Task<Empty> UpdateSyncState(string instrumentNumber, CommunicationSynchronizationState flag)
    {
        var session = sessionManager.GetSession(instrumentNumber);
        if (session != null)
        {
            if (session.SetSynchronizationFlag(flag))
            {
                // Notify that the session is fully synchronized
                await mediator.Publish(new CommunicationSessionSynchronizedEvent(session));
            }
        }

        return new Empty();
    }
}
