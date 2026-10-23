using MediatR;

namespace Pharos.Server.Application.Instrument;

public record ValidCurvesUpdatedEvent(string InstrumentNumber) : INotification;