using MediatR;

namespace Pharos.Server.Instrument.Application.Events;

internal record ModuleInformationReceivedEvent(int InstrumentId) : INotification;
