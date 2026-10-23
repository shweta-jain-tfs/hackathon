using MediatR;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// Send this command to control signal tower state on multiple instruments at once.
/// </summary>
public record MassControlSignalTowerCommand(IDictionary<string, ControlSignalTowerRequest> InstrumentSpecific, ControlSignalTowerRequest? AllInstruments) : IRequest;