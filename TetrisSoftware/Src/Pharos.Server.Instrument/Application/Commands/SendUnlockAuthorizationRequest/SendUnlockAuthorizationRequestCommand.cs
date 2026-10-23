using MediatR;
using Tetris.Protocol.Common;

namespace Pharos.Server.Instrument.Application.Commands.SendUnlockAuthorizationRequest;

internal sealed record SendUnlockAuthorizationRequestCommand(string InstrumentNumber, EnumInstrumentCompartmentWithLock CompartmentWithLock) : IRequest;