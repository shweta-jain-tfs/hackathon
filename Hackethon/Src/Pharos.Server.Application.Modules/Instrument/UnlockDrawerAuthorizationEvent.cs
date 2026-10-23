using MediatR;
using Tetris.Protocol.Common;

namespace Pharos.Server.Application.Instrument;
public record UnlockDrawerAuthorizationEvent(string InstrumentNumber, EnumInstrumentCompartmentWithLock Drawer) : INotification;