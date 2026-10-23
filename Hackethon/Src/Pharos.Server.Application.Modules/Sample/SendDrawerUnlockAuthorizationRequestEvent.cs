using MediatR;
using Tetris.Protocol.Common;

namespace Pharos.Server.Application.Sample;

public record SendDrawerUnlockAuthorizationRequestEvent(string InstrumentNumber, EnumInstrumentCompartmentWithLock CompartmentWithLock) : INotification;