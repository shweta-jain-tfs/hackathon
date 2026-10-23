using MediatR;
using Pharos.Contracts.Security;
using Pharos.Server.Application;
using Pharos.Server.Application.AuditTrail.Events;
using Pharos.Server.Application.Exceptions;
using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Security;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Infrastructure;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Common;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.UnlockCompartment;

internal record UnlockCompartmentCommand(string InstrumentNumber, EnumInstrumentCompartmentWithLock Compartment, Guid? UserId = null) : IInstrumentCommand, IRequest;

internal class UnlockCompartmentCommandHandler(ICommunicationSessionManager sessionManager,
    IInstrumentManager instrumentManager,
    ISender sender,
    IAuthorizationService authenticationService,
    ICurrentUserService currentUserService,
    IDateTime dateTime) : InstrumentCommandHandlerBase<UnlockCompartmentCommand>(sessionManager)
{
    public override async Task Handle(UnlockCompartmentCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId ?? request.UserId;
        if (currentUserId == null)
        {
            throw new InvalidOperationException("User ID is required to unlock a compartment.");
        }

        var instrument = instrumentManager.GetInstrument(request.InstrumentNumber);

        bool updateDrawerStateAuthorized;
        switch (request.Compartment)
        {
            case EnumInstrumentCompartmentWithLock.CompartmentReagentDrawer:
            case EnumInstrumentCompartmentWithLock.CompartmentSolidWasteDrawer:
                updateDrawerStateAuthorized = await authenticationService.AuthorizeUserAsync(currentUserId.Value, Permissions.ManageReagents);
                break;

            case EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1:
            case EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer2:
                updateDrawerStateAuthorized = await authenticationService.AuthorizeUserAsync(currentUserId.Value, Permissions.ManageSamples);
                await CreateAuditTrail(request, updateDrawerStateAuthorized, cancellationToken);
                break;

            case EnumInstrumentCompartmentWithLock.CompartmentHood:
                var permission = instrument.State == InstrumentState.Standby
                    ? Permissions.ManageInstruments
                    : Permissions.RunServiceTools;
                updateDrawerStateAuthorized = await authenticationService.AuthorizeUserAsync(currentUserId.Value, permission);
                break;

            default:
                updateDrawerStateAuthorized = false;
                break;
        }

        if (!updateDrawerStateAuthorized)
        {
            throw new NotAllowedException("User does not have permission to unlock the specified compartment.");
        }

        await base.Handle(request, cancellationToken);
    }

    protected override async Task Handle(UnlockCompartmentCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        await session.Connection.UnlockCompartmentAsync(new UnlockCompartmentRequest { Compartment = request.Compartment }, cancellationToken);
        await sender.Send(new CompartmentLockStateChangedCommand(request.InstrumentNumber, request.Compartment), cancellationToken);
    }

    private async Task CreateAuditTrail(UnlockCompartmentCommand request, bool updateDrawerStateAuthorized, CancellationToken cancellationToken)
    {
        var instrument = instrumentManager.GetInstrument(request.InstrumentNumber);
        var drawerType = request.Compartment == EnumInstrumentCompartmentWithLock.CompartmentSampleDrawer1 ? DrawerType.Sample1 : DrawerType.Sample2;

        if (updateDrawerStateAuthorized)
        {
            var unlockEvent = SampleDrawerEvents.SampleDrawerUnlockEvent.Create(instrument.SerialNumber, drawerType, dateTime.Now);
            await sender.Send(unlockEvent.ToCommand(), cancellationToken);
        }
        else
        {
            var unauthorizedAccessEvent = SampleDrawerEvents.SampleDrawerUnauthorizedUnlockAttemptEvent.Create(instrument.SerialNumber, drawerType, dateTime.Now);
            await sender.Send(unauthorizedAccessEvent.ToCommand(), cancellationToken);
        }
    }
}