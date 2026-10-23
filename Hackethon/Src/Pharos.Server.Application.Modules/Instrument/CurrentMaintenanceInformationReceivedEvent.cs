using MediatR;
using Tetris.Protocol.Pharos;

namespace Pharos.Server.Application.Instrument;

public record CurrentMaintenanceInformationReceivedEvent(string InstrumentNumber, CurrentMaintenanceInformation Message) : INotification;