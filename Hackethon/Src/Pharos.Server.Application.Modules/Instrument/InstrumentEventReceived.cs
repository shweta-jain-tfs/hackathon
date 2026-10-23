using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Instrument;

public record InstrumentEventReceived(string InstrumentNumber, Event Message) : INotification;