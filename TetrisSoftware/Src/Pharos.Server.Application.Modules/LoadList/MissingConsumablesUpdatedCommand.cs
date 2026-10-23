using MediatR;
using Tetris.Protocol.Pharos.Consumables;

namespace Pharos.Server.Application.LoadList;

public record MissingConsumablesUpdatedCommand(string InstrumentNumber, MissingConsumablesInfo MissingConsumablesInfo) : IRequest;