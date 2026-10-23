using MediatR;
using Tetris.Protocol.Instrument.Control;

namespace Pharos.Server.Application.TaskCard;

/// <summary>
/// Use this query to get the signal tower state (according to the present user task, alerts, etc.) for an instrument.
/// </summary>
public record GetSignalTowerStateQuery(string InstrumentNumber) : IRequest<ControlSignalTowerRequest>;