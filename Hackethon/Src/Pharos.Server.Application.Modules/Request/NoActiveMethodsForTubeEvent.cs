using MediatR;

namespace Pharos.Server.Application.Request;

/// <summary>
/// This event is published when Pharos tries to send tests to an instrument and detects that
/// no test order can be sent to a tube because all needed methods are inactive on that instrument.
/// </summary>
public record NoActiveMethodsForTubeEvent(string InstrumentNumber, IReadOnlyList<string> TubeIdList) : INotification;
