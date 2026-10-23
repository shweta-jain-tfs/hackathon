using MediatR;

namespace Pharos.Server.Application.Instrument;

public record MethodActivatedEvent(string InstrumentNumber, string MethodName, bool Active) : INotification;
