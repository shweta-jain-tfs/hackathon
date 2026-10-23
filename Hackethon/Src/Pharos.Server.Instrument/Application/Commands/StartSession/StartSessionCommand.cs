using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Pharos.Server.Application;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.SystemErrors;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Instrument.Application.Commands.CreateInstrument;
using Pharos.Server.Instrument.Application.Events;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Pharos;
using InstrumentState = Pharos.Server.Domain.Enums.InstrumentState;

namespace Pharos.Server.Instrument.Application.Commands.StartSession;

internal record StartSessionCommand(IPAddress IpAddress, Guid SessionId, string InstrumentNumber, string InstrumentProtocolVersion, string InstrumentSoftwareVersion, int InstrumentServicePort) : IRequest<EnumCompatibilityStatus>;

internal class StartSessionCommandHandler(
    ICommunicationSessionManager sessionManager,
    IInstrumentManager instrumentManager,
    IPharosVersionService pharosVersionService,
    ITetrisProtocolInformation protocolInformation,
    IInstrumentDbContext dbContext,
    IMediator mediator) : IRequestHandler<StartSessionCommand, EnumCompatibilityStatus>
{
    public async Task<EnumCompatibilityStatus> Handle(StartSessionCommand request, CancellationToken cancellationToken)
    {
        ValidateSessionId(request.SessionId);
        ValidateInstrumentAddressIsUnique(request.IpAddress, request.InstrumentServicePort, request.InstrumentNumber);

        var compatibilityStatus = GetCompatibilityStatus(request.InstrumentProtocolVersion);

        // Check if the instrument with this serial number had been added earlier.
        var instrument = (Domain.Instrument?)instrumentManager.FindInstrument(request.InstrumentNumber);

        if (instrument is null)
        {
            // new instrument that we have not seen before
            instrument = await mediator.Send(new CreateInstrumentCommand(request.IpAddress, request.InstrumentNumber, request.InstrumentSoftwareVersion), cancellationToken);
        }
        else
        {
            // If the instrument software call StartSession once again,
            // this means that we are resetting the communication session (cannot happen with the new instrument).
            await CloseStaleInstrumentSession(request.InstrumentNumber).ConfigureAwait(false);

            var dbInstrument = await dbContext.Instruments
                .Where(i => i.SerialNumber == request.InstrumentNumber)
                .FirstAsync(cancellationToken);

            var instrumentConfigurationUpdated =
                !Equals(dbInstrument.IpAddress, request.IpAddress) ||
                dbInstrument.InstrumentSoftwareVersion != request.InstrumentSoftwareVersion;

            if (instrumentConfigurationUpdated)
            {
                dbInstrument.IpAddress = request.IpAddress;
                dbInstrument.InstrumentSoftwareVersion = request.InstrumentSoftwareVersion;
                dbInstrument.AddDomainEvent(new InstrumentUpdatedEvent(dbInstrument, false));
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                instrument.IpAddress = request.IpAddress;
                instrument.InstrumentSoftwareVersion = request.InstrumentSoftwareVersion;
            }
        }

        if (compatibilityStatus != EnumCompatibilityStatus.FullyCompatible)
        {
            await mediator.Send(new CreatePharosErrorCommand(
                compatibilityStatus == EnumCompatibilityStatus.Incompatible ? PharosErrorCodes.InstrumentIncompatible : PharosErrorCodes.InstrumentLimitedCompatibility,
                ErrorClassification.Error,
                Parameters: [request.InstrumentSoftwareVersion, request.InstrumentProtocolVersion, pharosVersionService.Version, protocolInformation.VersionString],
                InstrumentNumber: request.InstrumentNumber,
                AllowMultiple: false), cancellationToken);
        }

        if (compatibilityStatus != EnumCompatibilityStatus.Incompatible)
        {
            // we don't start the session for an incompatible instrument, but we do for a limited or fully compatible one
            await sessionManager.StartSessionAsync(request.IpAddress, request.InstrumentServicePort, request.SessionId, request.InstrumentNumber, compatibilityStatus);

            instrument.State = compatibilityStatus == EnumCompatibilityStatus.FullyCompatible ? InstrumentState.Synchronizing : InstrumentState.Limited;
            await mediator.Publish(new InstrumentStateChangedEvent(instrument.Id, instrument.State), cancellationToken);

            // Start state synchronization with the instrument after the session is started.
            await mediator.Publish(new InstrumentConnectedEvent(request.SessionId, instrument.SerialNumber, compatibilityStatus), cancellationToken);
        }

        return compatibilityStatus;
    }

    private EnumCompatibilityStatus GetCompatibilityStatus(string instrumentProtocolVersion)
    {
        if (!Version.TryParse(instrumentProtocolVersion, out var instrProtocolVersion))
        {
            throw new ValidationException($"Invalid protocol version format: {instrumentProtocolVersion}");
        }

        var pharosVersion = protocolInformation.Version;

        // EnumCompatibilityStatus response is not supported prior to communication protocol version 4
        if (instrProtocolVersion.Major < 4)
        {
            return EnumCompatibilityStatus.Incompatible;
        }

        if (pharosVersion.Major == instrProtocolVersion.Major && pharosVersion.Minor == instrProtocolVersion.Minor)
        {
            return EnumCompatibilityStatus.FullyCompatible;
        }

        return EnumCompatibilityStatus.FilesApiOnly;
    }

    private void ValidateSessionId(Guid sessionId)
    {
        if (sessionManager.GetSession(sessionId) != null)
        {
            throw new ValidationException($"The communication session ID is not unique. Every {nameof(PharosService.PharosServiceBase.StartSession)} call must provide a unique 'sessionId' header value.");
        }
    }

    private void ValidateInstrumentAddressIsUnique(IPAddress ipAddress, int port, string instrumentNumber)
    {
        var session = sessionManager.GetSession(ipAddress, port);
        if (session != null && session.InstrumentNumber != instrumentNumber)
        {
            throw new ValidationException($"Cannot start session for the instrument with the serial number {instrumentNumber}. The address {ipAddress}:{port} is already taken by the instrument {session.InstrumentNumber}.");
        }
    }

    private async Task CloseStaleInstrumentSession(string instrumentNumber)
    {
        var session = sessionManager.GetSession(instrumentNumber);
        if (session != null)
        {
            await sessionManager.StopSessionAsync(session.SessionId);
        }
    }
}
