using MediatR;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// This event is published every time when an instrument drawer is unlocked, opened, closed, or locked.
/// </summary>
public record DrawerLockStateChanged(string InstrumentNumber, IReadOnlyList<DrawerLockState>? PreviousState, IReadOnlyList<DrawerLockState> CurrentState) : INotification;
