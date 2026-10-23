using MediatR;
using Tetris.Protocol.Instrument.TestOrder;

namespace Pharos.Server.Application.Instrument;

/// <summary>
/// eSend a command to update to add or update test orders on a specific instrument.
/// The request handler returns false if the message has not been sent (e.g. the instrument is offline).
/// </summary>
public record DeleteTestOrderCommand(string InstrumentNumber, DeleteTestOrderRequest TestOrderRequest) : IInstrumentCommand, IRequest;
